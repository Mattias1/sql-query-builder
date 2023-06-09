using FluentAssertions;
using SqlQueryBuilder.Builder;
using SqlQueryBuilder.Options;
using SqlQueryBuilder.Testing;
using Xunit;

namespace SqlQueryBuilder.Test.Builder;

public sealed class GroupByHavingQueriesTest {
    private IInitialQueryBuilder Query() => QueryBuilder.Init(new QueryBuilderOptions(new FakeSqlFlavor()) {
        DontParameterizeNumbers = false
    });

    [Fact]
    public void TestBasicGroupBy() {
        string sql = Query().Select("color").CountAllAs("colors").From("user")
            .GroupBy("color")
            .ToParameterizedSql();
        sql.Should().Be("select `color`, count(*) as `colors` from `user` group by `color`");
    }

    [Fact]
    public void TestGroupByMultipleSelectColumns() {
        string sql = Query().Select("color").Min("age").Max("age").From("user")
            .GroupBy("color")
            .ToParameterizedSql();
        sql.Should().Be("select `color`, min(`age`), max(`age`) from `user` group by `color`");
    }

    [Fact]
    public void TestGroupByMultipleGroupColumns() {
        string sql = Query().Select("color", "counter").AvgAs("age", "avg_age").From("user")
            .GroupBy("color", "counter")
            .ToParameterizedSql();
        sql.Should().Be("select `color`, `counter`, avg(`age`) as `avg_age` from `user` "
            + "group by `color`, `counter`");
    }

    [Fact]
    public void TestBasicHaving() {
        string sql = Query().Select("color").CountAllAs("colors").From("user")
            .GroupBy("color")
            .Having("colors").LtEq(50)
            .OrderByAsc("colors")
            .ToParameterizedSql();
        sql.Should().Be("select `color`, count(*) as `colors` from `user` "
            + "group by `color` having `colors` <= @p0 order by `colors` asc");
    }

    [Fact]
    public void TestAdvancedHaving() {
        string sql = Query().SelectAllFrom("user")
            .Having(q => q
                .Having("age").Gt(20)
                .AndHaving("counter").Gt(50)
            )
            .OrHaving(q => q
                .Having("age").Gt(15)
                .AndHaving("counter").Gt(100)
                .AndHaving(p => p
                    .NotHaving(r => r.Having("color").Like("%red%"))
                    .OrNotHaving(r => r.Having("color").Like("%blue%"))
                    .AndNotHaving(r => r.Having("color").Like("%green%"))
                )
            )
            .OrHaving("age").Eq(42)
            .ToParameterizedSql();

        sql.Should().Be("select `user`.* from `user` "
            + "having (`age` > @p0 and `counter` > @p1) "
            + "or (`age` > @p2 and `counter` > @p3 and ("
            + "not (`color` like @p4) or not (`color` like @p5) and not (`color` like @p6))"
            + ") "
            + "or `age` = @p7");
    }
}
