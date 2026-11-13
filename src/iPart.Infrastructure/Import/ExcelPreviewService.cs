using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using iPart.Core.Components;
using iPart.Core.Import;
using iPart.Core.Units;

namespace iPart.Infrastructure.Import;

public sealed class ExcelPreviewService(IImportReferenceReader referenceReader) : IExcelPreviewService
{
    public Task<ImportReport> AnalyzeAsync(string path, ImportOptions options, IProgress<ImportProgress>? progress = null,
        CancellationToken cancellationToken = default) => Task.Run(async () =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!string.Equals(Path.GetExtension(path), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Wybierz plik .xlsx.");
        progress?.Report(new("Odczyt słowników i kluczy z bazy (bez zapisu)", null, 0, 0));
        ImportReferences? references = null;
        string? referenceError = null;
        try { references = await referenceReader.ReadAsync(cancellationToken).ConfigureAwait(false); }
        catch (OperationCanceledException) { throw; }
        catch (Exception) { referenceError = "Nie udało się odczytać słowników i kluczy z bazy. Kolizje z bazą nie zostały sprawdzone; żaden rekord nie jest oznaczony jako gotowy."; }
        await using var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(input, cancellationToken));
        input.Position = 0;
        var sheets = new OpenXmlWorkbookReader().Read(input, progress, cancellationToken);
        var report = new ImportReport { FilePath = Path.GetFullPath(path), InputSha256 = hash, Options = options };
        report.Sheets.AddRange(sheets);
        new Mapping(report, references, referenceError, progress, cancellationToken).Run();
        cancellationToken.ThrowIfCancellationRequested();
        await using var verification = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (hash != Convert.ToHexString(await SHA256.HashDataAsync(verification, cancellationToken)))
            throw new IOException("Plik zmienił się w trakcie analizy. Wybierz stabilną kopię i ponów analizę.");
        progress?.Report(new("Analiza zakończona — bez zapisu do bazy", null, report.Records.Count, 100));
        return report;
    }, cancellationToken);

    private sealed class Mapping(ImportReport report, ImportReferences? references, string? referenceError,
        IProgress<ImportProgress>? progress, CancellationToken cancellationToken)
    {
        private static readonly string[] CommonHeaders = ["Part Number", "Mounting", "Case", "Footprint", "Notes", "Critical", "Value",
            "RC", "TT", "DL", "QL", "BGA", "TP", "THT", "preTHT", "pressFIT", "Mech", "BTC", "NS", "ThP", "NonS"];
        private static readonly string[] ResistorHeaders = ["Part Number", "Rezystancja", "Tolerancja", "Moc", "AEC-Q200", "ppm"];
        private static readonly string[] CapacitorHeaders = ["Part Number", "Pojemność", "Napięcie", "Dielektryk", "Tolerancja", "AEC-Q200"];
        private static readonly string[] CtceHeaders = ["Part Number", "Pojemność", "Napięcie", "Tolerancja", "ESR [mR]", "lowESR", "AEC-Q200", "Life (hs)"];
        private readonly List<ImportIssue> _blockingGlobal = [];
        private readonly Dictionary<string, Schema> _schemas = new(StringComparer.Ordinal);
        private readonly Dictionary<ImportPreviewRow, Dictionary<string, List<Proposal>>> _proposals = [];
        private sealed record Proposal(object Value, SourceCell Source);

        public void Run()
        {
            if (referenceError is not null) GlobalError("DatabaseUnavailable", referenceError, []);
            foreach (var sheet in report.Sheets)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var expected = sheet.Name switch { "partsData" => CommonHeaders, "resValue" => ResistorHeaders,
                    "capValue" => CapacitorHeaders, "ctceValue" => CtceHeaders, _ => [] };
                var schema = new Schema(sheet);
                _schemas[sheet.Name] = schema;
                if (expected.Length > 0)
                {
                    if (!schema.Columns.ContainsKey("Part Number")) GlobalError("MissingHeader", $"{sheet.Name}: brak jednoznacznego wymaganego nagłówka Part Number.", sheet.HeaderCells);
                    foreach (var group in schema.Duplicates)
                        GlobalError("DuplicateHeader", $"{sheet.Name}: powtórzony nagłówek „{group.Key}”; kolumny nie będą wybierane arbitralnie.", sheet.HeaderCells.Where(x => string.Equals(x.Value?.Trim(), group.Key, StringComparison.OrdinalIgnoreCase)).ToArray());
                    foreach (var missing in expected.Where(x => x != "Part Number" && !schema.Columns.ContainsKey(x)))
                        Problem(ImportSeverity.Warning, "MissingOptionalHeader", $"{sheet.Name}: nie ma opcjonalnej kolumny {missing}; dane tego pola nie są dostępne.", []);
                }
                var known = expected.ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (!report.Options.ConfirmThPToTp) known.Remove("ThP");
                if (!report.Options.ConfirmNonSToNs) known.Remove("NonS");
                foreach (var column in sheet.Rows.Where(x => !x.IsPreparedFormulaRow).SelectMany(x => x.Cells).GroupBy(x => x.Column))
                {
                    var header = sheet.Headers.GetValueOrDefault(column.Key);
                    if (header is not null && known.Contains(header)) continue;
                    var label = $"{sheet.Name}!{OpenXmlWorkbookReader.ColumnName(column.Key)}: {header ?? "(bez nagłówka)"}";
                    report.UnmappedColumns.Add(label);
                    Problem(ImportSeverity.Warning, "UnmappedColumn", $"Nieustalone mapowanie: {label}. Wszystkie {column.Count()} niepuste komórki zachowano w źródle raportu.", column.Take(1).ToArray());
                }
            }
            if (!_schemas.TryGetValue("partsData", out var parts))
            { GlobalError("MissingSheet", "Brak wymaganego arkusza partsData.", []); return; }
            foreach (var row in parts.Sheet.Rows.Where(x => x.Number > parts.Sheet.HeaderRow && !x.IsPreparedFormulaRow))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var keyCell = parts.Cell(row, "Part Number");
                if (keyCell is null || string.IsNullOrWhiteSpace(keyCell.Value))
                {
                    if (keyCell?.Formula is not null)
                        Problem(ImportSeverity.Error, "MpnFormulaWithoutCache", "Nie można ustalić Part Number: formuła bez zapisanego wyniku. Nie utworzono komponentu z domysłu.", [keyCell]);
                    if (row.Cells.Any(x => x.Formula is null && !string.IsNullOrWhiteSpace(x.Value)))
                        Problem(ImportSeverity.Warning, "RowWithoutMpn", "Wiersz bez Part Number nie jest komponentem; dane zachowano w podglądzie źródłowym.", row.Cells);
                    continue;
                }
                var preview = new ImportPreviewRow { SourceRow = row.Number, Component = new Component() };
                report.Records.Add(preview);
                preview.Sources.AddRange(row.Cells);
                _proposals[preview] = new(StringComparer.Ordinal);
                var mpn = Identifier(keyCell, preview);
                preview.Component.Mpn = mpn ?? keyCell.Value;
                Field(preview, "Mpn", keyCell);
                ReadCommon(preview, row, parts);
                ReportUnmapped(row, preview);
            }
            var byMpn = report.Records.GroupBy(x => x.Mpn, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.ToArray(), StringComparer.Ordinal);
            var duplicateGroups = report.Records.GroupBy(x => new ImportBusinessKey(x.Mpn, x.Component.ManufacturerId, x.Component.CustomerId)).Where(x => x.Count() > 1).ToArray();
            report.DuplicateGroups = duplicateGroups.Length;
            foreach (var group in duplicateGroups)
                Problem(ImportSeverity.Error, "DuplicateBusinessKey", $"Duplikat klucza biznesowego w {group.Count()} wierszach; producent i klient NULL nie są wildcardami.", group.SelectMany(x => x.FieldSources["Mpn"]).ToArray(), group.ToArray());
            foreach (var name in new[] { "resValue", "capValue", "ctceValue" })
            {
                if (!_schemas.TryGetValue(name, out var schema)) { Problem(ImportSeverity.Information, "OptionalSheetMissing", $"Brak opcjonalnego arkusza {name}.", []); continue; }
                foreach (var row in schema.Sheet.Rows.Where(x => x.Number > schema.Sheet.HeaderRow && !x.IsPreparedFormulaRow))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var cell = schema.Cell(row, "Part Number");
                    if (cell is null || string.IsNullOrWhiteSpace(cell.Value))
                    { report.UnmatchedAuxiliaryRows++; Problem(ImportSeverity.Warning, "AuxiliaryWithoutMpn", "Dane pomocnicze bez MPN zachowano bez dopasowania.", row.Cells); continue; }
                    var mpn = Identifier(cell, null);
                    cell.Mapping = "Klucz dopasowania MPN";
                    if (mpn is null || !byMpn.TryGetValue(mpn, out var matches))
                    {
                        report.UnmatchedAuxiliaryRows++;
                        Problem(ImportSeverity.Warning, "UnmatchedAuxiliary", "Nie znaleziono jednoznacznego komponentu partsData dla wiersza pomocniczego. Nie utworzono komponentu.", row.Cells);
                        continue;
                    }
                    foreach (var target in matches) target.Sources.AddRange(row.Cells);
                    if (matches.Length != 1)
                    {
                        report.UnmatchedAuxiliaryRows++;
                        Problem(ImportSeverity.Error, "AmbiguousJoin", "MPN pasuje do kilku wierszy partsData. Parametrów nie rozdzielono ani nie przypisano arbitralnie.", row.Cells, matches);
                        continue;
                    }
                    ReadParameters(matches[0], row, schema);
                    ReportUnmapped(row, matches[0]);
                }
            }
            var processed = 0;
            foreach (var target in report.Records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Merge(target);
                foreach (var global in _blockingGlobal) if (!target.Issues.Contains(global)) target.Issues.Add(global);
                if (references?.ExistingKeys.Contains(new(target.Mpn, target.Component.ManufacturerId, target.Component.CustomerId)) == true)
                { report.DatabaseCollisions++; Problem(ImportSeverity.Error, "DatabaseCollision", "Taki MPN, producent i klient istnieją już w bazie. Nie zaplanowano automatycznego scalenia.", target.FieldSources["Mpn"], [target]); }
                try { ComponentValidator.Validate(target.Component); }
                catch (ComponentValidationException exception) { Problem(ImportSeverity.Error, "ModelValidation", exception.Message, target.Sources, [target]); }
                if (target.Component.Resistor is not null)
                    target.GeneratedValue = new ComponentValueFormatter().FormatResistor(target.Component.Resistor,
                        target.Component.Qualifications.Count > 0 ? ["AEC-Q200"] : []);
                if (++processed % 250 == 0) progress?.Report(new("Walidacja i porównanie kluczy", "partsData", processed, 65 + processed * 30 / Math.Max(1, report.Records.Count)));
            }
        }

        private void ReadCommon(ImportPreviewRow target, SourceRow row, Schema schema)
        {
            foreach (var (header, property) in new[] { ("Case", "CaseName"), ("Footprint", "Footprint"), ("Notes", "Notes") })
                Map(target, schema.Cell(row, header), property, c => c.Value!, value => typeof(Component).GetProperty(property)!.SetValue(target.Component, value));
            Map(target, schema.Cell(row, "Critical"), "Critical", c => Boolean(c.Value!), v => target.Component.Critical = (bool)v);
            var mounting = schema.Cell(row, "Mounting");
            if (HasValue(mounting, target))
            {
                Field(target, "MountingMethodId", mounting!);
                var name = mounting!.Value!.Trim();
                if (name == "MECH") name = "Mech";
                target.Mounting = name;
                if (references?.MountingMethods.TryGetValue(name, out var id) == true) target.Component.MountingMethodId = id;
                else Problem(ImportSeverity.Error, "UnknownMounting", $"Nieznany sposób montażu „{name}”. Podgląd nie tworzy słownika.", [mounting], [target]);
            }
            var value = schema.Cell(row, "Value");
            if (value is not null)
            {
                value.Mapping = "Value źródłowe (tylko porównanie)";
                target.SourceValue = value.Value;
                if (value.Formula is not null && string.IsNullOrWhiteSpace(value.Value))
                    Problem(ImportSeverity.Warning, "FormulaWithoutCache", "Value jest formułą bez zapisanego wyniku. Nie obliczano jej; pozostałe dane odczytano.", [value], [target]);
            }
            foreach (var (header, property) in new[] { ("RC", "RC"), ("TT", "TT"), ("DL", "DL"), ("QL", "QL"), ("BGA", "BGA"), ("TP", "TP"),
                         ("THT", "THT"), ("preTHT", "PreTHT"), ("pressFIT", "PressFIT"), ("Mech", "MECH"), ("BTC", "BTC"), ("NS", "NS"), ("ThP", "TP"), ("NonS", "NS") })
            {
                var cell = schema.Cell(row, header);
                if (header == "ThP" && !report.Options.ConfirmThPToTp || header == "NonS" && !report.Options.ConfirmNonSToNs)
                {
                    if (cell is not null) Problem(ImportSeverity.Warning, "MappingConfirmationRequired", $"Propozycja {header} → {property} wymaga zatwierdzenia w zakładce Mapowanie i ponownej analizy.", [cell], [target]);
                    continue;
                }
                Propose(target, cell, "Pads." + property, c => property is "BTC" or "NS" ? Boolean(c.Value!) : Integer(c.Value!));
            }
        }

        private void ReadParameters(ImportPreviewRow target, SourceRow row, Schema schema)
        {
            var resistor = schema.Sheet.Name == "resValue";
            if (resistor)
            {
                Propose(target, schema.Cell(row, "Rezystancja"), "Resistor.ResistanceOhms", c => ParameterInput.ResistanceOhms(WithUnit(c, "R")));
                Propose(target, schema.Cell(row, "Tolerancja"), "Resistor.TolerancePercent", c => Percent(c));
                Propose(target, schema.Cell(row, "Moc"), "Resistor.PowerWatts", c => ParameterInput.PowerWatts(WithUnit(c, "W")));
                Propose(target, schema.Cell(row, "ppm"), "Resistor.TemperatureCoefficientPpmPerC", c => Ppm(c.Value!));
            }
            else
            {
                Propose(target, schema.Cell(row, "Pojemność"), "Capacitor.CapacitancePf", c => ParameterInput.CapacitancePf(WithUnit(c, "pF")));
                Propose(target, schema.Cell(row, "Napięcie"), "Capacitor.RatedVoltageV", c => ParameterInput.VoltageV(WithUnit(c, "V")));
                Propose(target, schema.Cell(row, "Dielektryk"), "Capacitor.Dielectric", c => c.Value!);
                var tolerance = schema.Cell(row, "Tolerancja");
                if (tolerance is not null)
                {
                    var absolute = tolerance.Kind == ExcelCellKind.Text && !string.IsNullOrWhiteSpace(tolerance.Value) && !tolerance.Value.Trim().EndsWith('%');
                    Propose(target, tolerance, absolute ? "Capacitor.AbsoluteTolerancePf" : "Capacitor.TolerancePercent",
                        c => absolute ? ParameterInput.CapacitancePf(c.Value!) : Percent(c));
                }
                Propose(target, schema.Cell(row, "ESR [mR]"), "Capacitor.EsrMilliOhms", c => Esr(c.Value!));
                Propose(target, schema.Cell(row, "lowESR"), "Capacitor.LowEsr", c => c.Value!.Trim() == "lowESR" || Boolean(c.Value!));
                Propose(target, schema.Cell(row, "Life (hs)"), "Capacitor.LifetimeHours", c => Integer(c.Value!));
            }
            Propose(target, schema.Cell(row, "AEC-Q200"), "AecQ200", c => c.Value!.Trim() == "AEC-Q200" || Boolean(c.Value!));
        }

        private void Propose(ImportPreviewRow target, SourceCell? cell, string path, Func<SourceCell, object> parse)
        {
            if (cell is null) return;
            if (!HasValue(cell, target)) { cell.Mapping = path; return; }
            Field(target, path, cell);
            try
            {
                var value = parse(cell);
                if (path != "AecQ200") ValidateProperty(path, value);
                if (!_proposals[target].TryGetValue(path, out var values)) _proposals[target][path] = values = [];
                values.Add(new(value, cell));
            }
            catch (Exception exception) when (exception is QuantityInputException or ComponentValidationException or FormatException or OverflowException)
            { Problem(ImportSeverity.Error, "InvalidParameter", exception.Message, [cell], [target]); }
        }

        private void Merge(ImportPreviewRow target)
        {
            foreach (var (path, proposals) in _proposals[target])
            {
                var sources = proposals.Select(x => x.Source).ToArray();
                var distinct = proposals.Select(x => x.Value).Distinct().ToArray();
                if (distinct.Length != 1)
                { Problem(ImportSeverity.Error, "ParameterConflict", $"Sprzeczne propozycje dla {path}; nie wybrano żadnej wartości.", sources, [target]); continue; }
                if (proposals.Count > 1) Problem(ImportSeverity.Information, "RepeatedParameter", $"Powtórzone zgodne dane dla {path}; zachowano wszystkie źródła.", sources, [target]);
                var value = distinct.Single();
                if (path == "AecQ200")
                {
                    if ((bool)value)
                    {
                        if (references?.AecQualifications.TryGetValue("AEC-Q200", out var id) == true)
                            target.Component.Qualifications.Add(new() { QualificationId = id });
                        else Problem(ImportSeverity.Error, "UnknownQualification", "Brak AEC-Q200 w odczytanym słowniku; nie utworzono pozycji słownika.", sources, [target]);
                    }
                    continue;
                }
                var pieces = path.Split('.');
                var container = EnsureSet(target.Component, pieces[0]);
                container.GetType().GetProperty(pieces[1])!.SetValue(container, value);
            }
            if (target.Component.Resistor is not null && target.Component.Capacitor is not null)
                Problem(ImportSeverity.Error, "ParameterTypeConflict", "Jedno MPN ma propozycje parametrów rezystora i kondensatora. Wymagane rozstrzygnięcie typu.", target.Sources, [target]);
        }

        private void Map(ImportPreviewRow target, SourceCell? cell, string path, Func<SourceCell, object> parse, Action<object> assign)
        {
            if (cell is null) return;
            Field(target, path, cell);
            if (!HasValue(cell, target)) return;
            try { var value = parse(cell); ValidateProperty(path, value); assign(value); }
            catch (Exception exception) when (exception is QuantityInputException or ComponentValidationException)
            { Problem(ImportSeverity.Error, "InvalidValue", exception.Message, [cell], [target]); }
        }

        private static object EnsureSet(Component component, string set)
        {
            var property = typeof(Component).GetProperty(set)!;
            if (property.GetValue(component) is { } existing) return existing;
            var value = Activator.CreateInstance(property.PropertyType)!;
            property.SetValue(component, value);
            ((IComponentDependent)value).ComponentId = component.Id;
            return value;
        }

        private static void ValidateProperty(string path, object value)
        {
            var parts = path.Split('.');
            var type = parts.Length == 1 ? typeof(Component) : typeof(Component).GetProperty(parts[0])!.PropertyType;
            var name = parts.Last();
            var property = type.GetProperty(name)!;
            if (property.GetCustomAttribute<DecimalSpecAttribute>() is { } spec && value is decimal number)
                DecimalRules.Validate(number, spec.Precision, spec.Scale, path, spec.AllowNegative);
            if (value is string text && text.Length > ComponentValidator.TextLimit(name))
                throw new ComponentValidationException($"{path}: przekroczona maksymalna długość {ComponentValidator.TextLimit(name)} znaków.");
            if (type == typeof(ComponentPads))
            {
                var pads = new ComponentPads(); property.SetValue(pads, value); ComponentValidator.ValidatePads(pads);
            }
        }

        private bool HasValue(SourceCell? cell, ImportPreviewRow? target)
        {
            if (cell is null) return false;
            if (cell.Kind == ExcelCellKind.Error)
            { Problem(ImportSeverity.Error, "ExcelError", "Komórka zawiera błąd Excela; wartości nie zinterpretowano.", [cell], target is null ? null : [target]); return false; }
            if (cell.Formula is not null && string.IsNullOrWhiteSpace(cell.Value))
            { Problem(ImportSeverity.Error, "FormulaWithoutCache", "Brak zapisanego wyniku formuły dla mapowanego pola. Formuły nie wykonano.", [cell], target is null ? null : [target]); return false; }
            return !string.IsNullOrWhiteSpace(cell.Value);
        }

        private string? Identifier(SourceCell cell, ImportPreviewRow? target)
        {
            if (!HasValue(cell, target)) return null;
            if (cell.Kind == ExcelCellKind.Text) return MpnRules.Normalize(cell.Value!);
            try
            {
                if (cell.Kind != ExcelCellKind.Number) throw new QuantityInputException("MPN musi być tekstem albo jednoznacznie odtwarzalną liczbą, nie wartością logiczną.");
                var number = QuantityParser.ParseNumber(cell.Value!);
                if (decimal.Truncate(number) != number) throw new QuantityInputException("Liczbowy MPN nie jest liczbą całkowitą; nie odtworzono oznaczenia.");
                var digits = number.ToString("0", CultureInfo.InvariantCulture);
                if (digits.Length > 15) throw new QuantityInputException("Liczbowy MPN przekracza 15 cyfr dokładności Excela. Oryginalnego identyfikatora nie można odtworzyć pewnie.");
                var format = cell.NumberFormat ?? "General";
                if (format.Length > 0 && format.All(x => x == '0'))
                {
                    var formatted = digits.PadLeft(format.Length, '0');
                    Problem(ImportSeverity.Information, "NumericMpnFormatted", "Liczbowy MPN odtworzono według jawnej maski zer; oryginalną liczbę i format zachowano.", [cell], target is null ? null : [target]);
                    return formatted;
                }
                if (format is not "General" and not "@") throw new QuantityInputException("Nieobsługiwany format liczbowego MPN. Nie zgadywano identyfikatora.");
                Problem(ImportSeverity.Warning, "NumericMpn", "MPN zapisano liczbowo bez maski zer. Zachowano widoczne cyfry; wcześniejszych utraconych zer nie da się ustalić.", [cell], target is null ? null : [target]);
                return digits;
            }
            catch (QuantityInputException exception)
            { Problem(ImportSeverity.Error, "UncertainMpn", exception.Message, [cell], target is null ? null : [target]); return null; }
        }

        private static string WithUnit(SourceCell cell, string unit) => cell.Kind == ExcelCellKind.Number ? cell.Value + unit : cell.Value!;
        private static decimal Percent(SourceCell cell)
        {
            var text = cell.Value!.Trim();
            if (text.EndsWith('%')) return QuantityParser.ParseNumber(text[..^1].Trim());
            if (cell.Kind != ExcelCellKind.Number) throw new QuantityInputException("Tolerancja tekstowa wymaga % albo jawnej jednostki pojemności.");
            var number = QuantityParser.ParseNumber(text);
            return cell.NumberFormat?.Contains('%') == true ? checked(number * 100) : number;
        }
        private static decimal Ppm(string input)
        {
            var text = input.Trim();
            foreach (var suffix in new[] { "ppm/°C", "ppm" }) if (text.EndsWith(suffix, StringComparison.Ordinal)) { text = text[..^suffix.Length].Trim(); break; }
            return text.StartsWith('-') ? -QuantityParser.ParseNumber(text[1..]) : QuantityParser.ParseNumber(text);
        }
        private static decimal Esr(string input)
        {
            var text = input.Trim();
            if (text.StartsWith("ESR", StringComparison.Ordinal)) return ParameterInput.EsrMilliOhms(text);
            if (text.EndsWith("mR", StringComparison.Ordinal) || text.EndsWith("mΩ", StringComparison.Ordinal)) text = text[..^2].Trim();
            var value = QuantityParser.ParseNumber(text); DecimalRules.Validate(value, 28, 9, "EsrMilliOhms"); return value;
        }
        private static int Integer(string text)
        {
            var value = QuantityParser.ParseNumber(text.Trim());
            if (decimal.Truncate(value) != value || value > int.MaxValue) throw new QuantityInputException("Wymagana nieujemna liczba całkowita w zakresie int.");
            return (int)value;
        }
        private static bool Boolean(string text) => text.Trim().ToLowerInvariant() switch
        {
            "true" or "1" or "tak" or "yes" => true, "false" or "0" or "nie" or "no" => false,
            _ => throw new QuantityInputException("Nieznana wartość logiczna. Dozwolone true/false, 1/0, tak/nie, yes/no.")
        };

        private static void Field(ImportPreviewRow target, string path, SourceCell cell)
        {
            cell.Mapping = path;
            if (!target.FieldSources.TryGetValue(path, out var sources)) target.FieldSources[path] = sources = [];
            if (!sources.Contains(cell)) sources.Add(cell);
        }
        private void ReportUnmapped(SourceRow row, ImportPreviewRow target)
        {
            var cells = row.Cells.Where(c => c.Mapping is null).ToArray();
            if (cells.Length > 0) Problem(ImportSeverity.Warning, "UnmappedData", "Nieustalone pola/historyczne informacje pozostają wyłącznie w źródle podglądu; nie są historią komponentu.", cells, [target]);
        }
        private void GlobalError(string code, string message, IReadOnlyList<SourceCell> sources)
        { _blockingGlobal.Add(Problem(ImportSeverity.Error, code, message, sources)); }
        private ImportIssue Problem(ImportSeverity severity, string code, string message, IReadOnlyList<SourceCell> sources, ImportPreviewRow[]? targets = null)
        {
            var issue = new ImportIssue(severity, code, message, sources, targets?.Length == 1 ? targets[0].SourceRow : null);
            report.Issues.Add(issue);
            if (targets is not null) foreach (var target in targets) target.Issues.Add(issue);
            return issue;
        }
        private sealed class Schema
        {
            public SourceSheet Sheet { get; }
            public Dictionary<string, int> Columns { get; } = new(StringComparer.OrdinalIgnoreCase);
            public IGrouping<string, KeyValuePair<int, string>>[] Duplicates { get; }
            public Schema(SourceSheet sheet)
            {
                Sheet = sheet;
                var groups = sheet.Headers.GroupBy(x => x.Value.Trim(), StringComparer.OrdinalIgnoreCase).ToArray();
                Duplicates = groups.Where(x => x.Count() > 1).ToArray();
                foreach (var group in groups.Where(x => x.Count() == 1)) Columns[group.Key] = group.Single().Key;
            }
            public SourceCell? Cell(SourceRow row, string header) => Columns.TryGetValue(header, out var column) ? row.Cells.FirstOrDefault(x => x.Column == column) : null;
        }
    }
}
