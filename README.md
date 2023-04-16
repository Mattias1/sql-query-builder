SQL Query builder
==================
A lightweight querybuilder for my database interactions, using Dapper internally.

I wanted a small, lightweight builder to make simple sql queries. I couldn't find anything
that I liked, so I made my own.


Examples
---------
A simple select query:
``` csharp
using SqlQueryBuilder.Builder;
using SqlQueryBuilder.MySql;

public IInitialQueryBuilder Query() {
    var sqlFlavor = new MySqlFlavor("localhost", "sql_user", "sql_password", "sql_database");
    return QueryBuilder.Init(sqlFlavor);
}

public IReadOnlyList<UserTable> Search(string name) {
    return Query()
        .SelectAllFrom("user")
        .Where("username").Like($"%{name}%")
        .OrderByDesc("created_at")
        .List<UserTable>();
    // Executes "select `user`.* from `user` where `username` like @p0 order by `created_at` desc"
}

public class UserTable {
    public long Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public LocalDateTime CreatedAt { get; set; }
}
```

Or an update query:
``` csharp
public bool SaveUser(UserTable item) {
    return Query()
        .Update("user")
        .SetFrom(item)
        .Where("id").Is(42)
        .Execute();
    // Executes "update `user` set `id` = 42, `username` = @p0, `email` = @p1, `created_at` = @p2 where `id` = 42"
}
```
Note that the id is not parameterized, because it's a `long` type, and therefore safe. This will
give you a performance boost for large where in lists.
Also note that if you forget the where clause, it'll throw an exception.
You can turn these [options](options.md) off if you want.

Note also that this assumes Dapper can deal with snake case and NodaTime objects. You can enable
that with something like: `QueryBuilderOptions.SetupDapperWithSnakeCaseAndNodaTime();`

A more complicated example:
``` csharp
public async Task<IReadOnlyList<GroupingTableStructure>> NewUsersWithManyRoles() {
    var query = Query()
        .Select().Column("u.id").Column("u.username").CountAs("r.id", "roles")
        .FromAs("user", "u")
        .JoinAs("role_user", "ru", "u.id", "ru.user_id")
        .JoinAs("role", "r", "ru.role_id", "r.id")
        .Where(q => q
            .Where("u.created_at").Gt(new LocalDate(2020, 02, 29))
            .Or("username").Is("moderator") // As benchmark
        )
        .AndNot(q => q
            .Where("u.id").Is(1)
            .Or("u.username").Is("admin")
        )
        .GroupBy("u.id", "u.username")
        .Having("roles").GtEq(3)
        .OrderByAsc("roles");

    string rawUnsafeSql = query.ToUnsafeSql();

    // The executed sql, with parameters inserted for debugging purposes, shows us the following:
    // select `u`.`id`, `u`.`username`, count(`r`.`id`) as `roles`
    // from `user` as `u`
    // join `role_user` as `ru` on `u`.`id` = `ru`.`user_id`
    // join `role` as `r` on `ru`.`role_id` = `r`.`id`
    // where (
    //     `u`.`created_at` >= '2020-03-01'
    //     or `username` = 'moderator'
    // )
    // and not (
    //     `u`.`id` = 1
    //     or `u`.`username` = 'admin'
    // )
    // group by `u`.`id`, `u`.`username`
    // having `roles` >= 3
    // order by `roles` asc

    return await query.ListAsync<GroupingTableStructure>();
}
```
Note that the date check `if date > feb 29` is transformed to `if date >= march 01`, to make sure
that noon feb 29 for example is not included in the check. This is only done for a `LocalDate`, not
for any other date types, like `LocalDateTime` or `System.DateTime` for example.
Again, if you don't like this, you can turn the [option](options.md) off.
