namespace ChefBot.Api.Database;

/// <summary>Where ChefBot's PostgreSQL database comes from (the Database:Mode setting).</summary>
public enum DatabaseMode
{
    /// <summary>ChefBot runs its own PostgreSQL, from the copy shipped with it, keeping the data under Data:Root.</summary>
    Bundled,

    /// <summary>ChefBot connects to a PostgreSQL server someone else runs, given by ConnectionStrings:ChefBot.</summary>
    External,
}
