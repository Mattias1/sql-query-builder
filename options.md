SQL Query builder options
==========================

There are some options that you choose to use. By default they are all enabled.

You can change or disble options like this:
``` csharp
public IInitialQueryBuilder Query() {
    var sqlFlavor = new SqlFlavor(...);
    return QueryBuilder.Init(new QueryBuilderOptions(sqlFlavor) {
        UseSmartDates = false
    });
}
```


### ColumnFormat
This is used to specify how the Insert and Update queries format the columns.

Provided options are `IdentityColumnFormat` and `CamelToSnakeColumnFormat` (default).
You can add your own format if you want.
For inspiration, take a look
[here](https://github.com/Mattias1/sql-query-builder/blob/master/SqlQueryBuilder.Core/Options/CamelToSnakeColumnFormat.cs).

**Note** that Dapper by default cannot deal with snake case and NodaTime objects. You can enable that
globally with something like: `QueryBuilderOptions.SetupDapperWithSnakeCaseAndNodaTime();`


### UseSmartDates
If enabled this modifies date logic to be more sensible. For example:
``` csharp
.Where("created_at").LtEq(new LocalDate(2020, 02, 29))
.Where("created_at").Gt(new LocalDate(2020, 02, 29))
```
will result in:
``` sql
where created_at < '2020-03-01'
and created_at >= '2020-03-01'
```

This only works for `LocalDate` objects, and only for the `Gt` and `LtEq` operators.


### UseOverprotectiveSqlInjectionDefence
If enabled this throws exceptions when you use characters that can potentially be used in sql
injections. Like semicolons (`;`), double dashes (`--`) or backticks (`` ` ``) inside table or
column names.


### AddParameterizedSqlToException
If enabled this adds the (parameterized) sql string to exceptions.


### DontParameterizeNumbers
If enabled this will not parameterize numbers and booleans.
For example, `.Where("number").Is(42)` will result in `where number = 42` directly, instead of
using a paremterized value like `where number = @p0`.


### GuardForForgottenWhere
If enabled this will throw an exception when you build an update or delete query without a where
clause. This can be bypassed by using the `.WithoutWhere()` method.


### WrapFieldNames
If enabled this will wrap table names and column names with backticks (`` ` ``). Or whatever
characters are appropriate for the current sql dialect.
For example `.Where("table.from").Is(42)` will result in ``where `table`.`from` = 42``
instead of `where table.from = 42`.
