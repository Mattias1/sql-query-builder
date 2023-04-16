using AdaskoTheBeAsT.Dapper.NodaTime;
using NodaTime;

namespace SqlQueryBuilder.Options;

public sealed class QueryBuilderOptions {
    public ISqlFlavor SqlFlavor { get; set; }
    public IColumnFormat ColumnFormat { get; set; }
    public bool UseSmartDates { get; set; }
    public bool UseOverprotectiveSqlInjectionDefence { get; set; }
    public bool AddParameterizedSqlToException { get; set; }
    public bool DontParameterizeNumbers { get; set; }
    public bool GuardForForgottenWhere { get; set; }
    public bool WrapFieldNames { get; set; }

    public QueryBuilderOptions(ISqlFlavor sqlFlavor) : this(sqlFlavor, Default) { }
    public QueryBuilderOptions(ISqlFlavor sqlFlavor, IColumnFormat columnFormat)
        : this(sqlFlavor, columnFormat, useSmartDates: true, useOverprotectiveDefence: true, addSqlToException: true,
              dontParameterizeNumbers: true, guardForForgottenWhere: true, wrapFieldNames: true) { }
    public QueryBuilderOptions(ISqlFlavor sqlFlavor, IColumnFormat columnFormat,
            bool useSmartDates, bool useOverprotectiveDefence, bool addSqlToException,
            bool dontParameterizeNumbers, bool guardForForgottenWhere, bool wrapFieldNames) {
        SqlFlavor = sqlFlavor;
        ColumnFormat = columnFormat;
        UseSmartDates = useSmartDates;
        UseOverprotectiveSqlInjectionDefence = useOverprotectiveDefence;
        AddParameterizedSqlToException = addSqlToException;
        DontParameterizeNumbers = dontParameterizeNumbers;
        GuardForForgottenWhere = guardForForgottenWhere;
        WrapFieldNames = wrapFieldNames;
    }

    public static QueryBuilderOptions SmartPreset(ISqlFlavor sqlFlavor) => SmartPreset(sqlFlavor, Default);
    public static QueryBuilderOptions SmartPreset(ISqlFlavor sqlFlavor, IColumnFormat columnFormat) {
        return new QueryBuilderOptions(sqlFlavor, columnFormat,
            useSmartDates: true, useOverprotectiveDefence: true, addSqlToException: true,
            dontParameterizeNumbers: true, guardForForgottenWhere: true, wrapFieldNames: true);
    }

    public static QueryBuilderOptions PlainPreset(ISqlFlavor sqlFlavor) => PlainPreset(sqlFlavor, None);
    public static QueryBuilderOptions PlainPreset(ISqlFlavor sqlFlavor, IColumnFormat columnFormat) {
        return new QueryBuilderOptions(sqlFlavor, columnFormat,
            useSmartDates: false, useOverprotectiveDefence: false, addSqlToException: false,
            dontParameterizeNumbers: false, guardForForgottenWhere: false, wrapFieldNames: false);
    }

    public static IColumnFormat Default => CamelToSnakeCase;
    public static IColumnFormat None => new IdentityColumnFormat();
    public static IColumnFormat CamelToSnakeCase => new CamelToSnakeColumnFormat();

    public QueryBuilderOptions Clone() {
        return new QueryBuilderOptions(
            SqlFlavor, ColumnFormat,
            UseSmartDates, UseOverprotectiveSqlInjectionDefence, AddParameterizedSqlToException,
            DontParameterizeNumbers, GuardForForgottenWhere, WrapFieldNames
        );
    }

    public static void SetupDapperWithSnakeCaseAndNodaTime() {
        SetupDapperWithSnakeCase();
        SetupDapperWithNodaTime();
    }
    public static void SetupDapperWithSnakeCase() => Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    public static void SetupDapperWithNodaTime() => DapperNodaTimeSetup.Register(DateTimeZoneProviders.Tzdb);
}
