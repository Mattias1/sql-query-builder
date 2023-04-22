using FluentAssertions;
using NodaTime;
using SqlQueryBuilder.Builder;
using SqlQueryBuilder.Testing;
using Xunit;

namespace SqlQueryBuilder.Test.Builder;

public sealed class SubqueriesTest {
    private IInitialQueryBuilder Query() => QueryBuilder.Init(new FakeSqlFlavor());

    [Fact]
    public void TestWhereExists() {
        string sql = Query().SelectAllFrom("user")
            .WhereExists(q => q
                .SelectAllFrom("statistics")
                .Where("text_statistic").Like("Test")
            )
            .ToUnsafeSql();
        sql.Should().Be("select `user`.* from `user` where exists ("
            + "select `statistics`.* from `statistics` where `text_statistic` like 'Test'"
            + ")");
    }

    [Fact]
    public void TestWhereNotExistsWithValues() {
        string sql = Query().SelectAllFrom("user")
            .WhereNotExists(q => q
                .SelectAllFrom("statistics")
                .Where("text_statistic").Like("Test")
            )
            .ToUnsafeSql();
        sql.Should().Be("select `user`.* from `user` where not exists ("
            + "select `statistics`.* from `statistics` where `text_statistic` like 'Test'"
            + ")");
    }

    [Fact]
    public void TestWhereIn() {
        string sql = Query().SelectAllFrom("user")
            .Where("id").In(q => q
                .Select("user_id")
                .From("statistics")
                .Where("some_statistic").Gt(42)
            )
            .ToParameterizedSql();
        sql.Should().Be("select `user`.* from `user` where `id` in ("
            + "select `user_id` from `statistics` where `some_statistic` > 42"
            + ")");
    }

    [Fact]
    public void TestWhereNotInWithValues() {
        string sql = Query().SelectAllFrom("user")
            .Where("id").NotIn(q => q
                .Select("user_id")
                .From("statistics")
                .Where("text_statistic").Like("Test")
            )
            .ToUnsafeSql();
        sql.Should().Be("select `user`.* from `user` where `id` not in ("
            + "select `user_id` from `statistics` where `text_statistic` like 'Test'"
            + ")");
    }

    [Fact]
    public void TestWhereEqSubquery() {
        string sql = Query().Select("user_id")
            .From("statistics")
            .Where("counter").Eq(q => q
                .Select().Max("counter")
                .From("statistics")
            )
            .ToUnsafeSql();
        sql.Should().Be("select `user_id` from `statistics` where `counter` = ("
            + "select max(`counter`) from `statistics`"
            + ")");
    }

    [Fact]
    public void TestWhereGtSubquery() {
        string sql = Query().Select("user_id")
            .From("statistics")
            .Where("counter").Gt(q => q
                .Select().Avg("counter")
                .From("statistics")
            )
            .ToUnsafeSql();
        sql.Should().Be("select `user_id` from `statistics` where `counter` > ("
            + "select avg(`counter`) from `statistics`"
            + ")");
    }

    [Fact]
    public void TestInsertIntoSubquery() {
        string sql = Query().InsertInto("user_backup")
            .Columns("id", "username", "total_things")
            .Select().Column("user_id").Column("username").SelectSubqueryAs("total_things", q => q
                .Select().CountAll()
                .From("thing")
                .Where("thing.user_id").IsColumn("user.id")
            )
            .From("user")
            .Where("last_active").Gt(new LocalDate(2020, 02, 02))
            .ToUnsafeSql();
        sql.Should().Be("insert into `user_backup` (`id`, `username`, `total_things`) "
            + "select `user_id`, `username`, ("
            + "select count(*) from `thing` where `thing`.`user_id` = `user`.`id`"
            + ") as `total_things` from `user` where `last_active` >= '2020-02-03'");
    }

    [Fact]
    public void TestUpdateFromSubquery() {
        string sql = Query().Update("my_table")
            .SetColumnToColumn("col1", "other.col1")
            .SetColumnToColumn("col2", "other.col2")
            .SetColumn("col3", 0)
            .FromAs("other", q => q
                .Select("col1").Column("col2")
                .From("other_table")
            )
            .Where("my_table.id").IsColumn("other_table.id")
            .ToParameterizedSql();
        sql.Should().Be("update `my_table` "
            + "set `col1` = `other`.`col1`, `col2` = `other`.`col2`, `col3` = 0 "
            + "from ("
            + "select `col1`, `col2` from `other_table`"
            + ") as `other` where `my_table`.`id` = `other_table`.`id`");
    }

    [Fact]
    public void TestSubQueryParameterOrderInOneGo() {
        string sql = Query().SelectAllFrom("user")
            .Where("user.username").Eq("grandpa")
            .OrWhere("user.counter").Eq(q => q
                .Select().Max("counter")
                .From("statistics")
                .Where("type").Is("birthdays")
            )
            .OrWhere("user.created_at").Lt(new LocalDate(1970, 01, 01))
            .ToParameterizedSql();
        sql.Should().Be("select `user`.* from `user` "
            + "where `user`.`username` = @p0 "
            + "or `user`.`counter` = (select max(`counter`) from `statistics` where `type` = @p1) "
            + "or `user`.`created_at` < @p2");
    }

    [Fact]
    public void TestSubQueryParameterOrderInMultiplePasses() {
        var query = Query().SelectAllFrom("user");

        query.Where("username").Eq("grandpa");

        query.OrWhere("counter").Eq(subquery => {
            // Admittedly, this is not a very pretty thing to do, but it's valid syntactically, so it should work
            query.OrWhere("created_at").Eq(new LocalDate(1971, 01, 01));

            subquery.Select().Max("counter").From("statistics");

            query.OrWhere("created_at").Eq(new LocalDate(1972, 01, 01));

            subquery.Cast().Where("type").Is("birthdays");

            query.OrWhere("created_at").Eq(new LocalDate(1973, 01, 01));

            return subquery.Cast();
        });

        query.OrWhere("created_at").Eq(new LocalDate(1974, 01, 01));

        string sql = query.ToParameterizedSql();

        sql.Should().Be("select `user`.* from `user` "
            + "where `username` = @p0 "
            + "or `created_at` = @p1 "
            + "or `created_at` = @p2 "
            + "or `created_at` = @p3 "
            + "or `counter` = (select max(`counter`) from `statistics` where `type` = @p4) "
            + "or `created_at` = @p5");
    }

    [Fact]
    public void TestSubQueryParameterOrderRecursively() {
        var query = Query().SelectAllFrom("user");

        query.Where("color").Eq("first");

        query.OrWhere("statistic_thing").Eq(subquery => {
            // Right... I hope no one actually does this. But again, it should work.
            query.OrWhere("color").Eq("second");

            subquery.Select().Max("thing").From("statistics")
                .Where("type").Is("sub1");

            query.OrWhere("color").Eq("third");

            subquery.Cast().And("thing").NotEq(subSubquery => {
                query.OrWhere("color").Eq("fourth");

                subSubquery.Select().Min("thing").From("statistics")
                    .Where("type").Is("sub2");

                query.OrWhere("color").Eq("fifth");

                return subSubquery.Cast();
            });

            query.OrWhere("color").Eq("sixth");

            return subquery.Cast();
        });

        query.OrWhere("color").Eq("seventh");

        string sql = query.ToUnsafeSql();

        sql.Should().Be("select `user`.* from `user` "
            + "where `color` = 'first' "
            + "or `color` = 'second' "
            + "or `color` = 'third' "
            + "or `color` = 'fourth' "
            + "or `color` = 'fifth' "
            + "or `color` = 'sixth' "
            + "or `statistic_thing` = ("
            + "select max(`thing`) from `statistics` where `type` = 'sub1' "
            + "and `thing` != (select min(`thing`) from `statistics` where `type` = 'sub2')"
            + ") "
            + "or `color` = 'seventh'");
    }
}
