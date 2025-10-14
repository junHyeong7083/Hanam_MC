using System;
using System.IO;
using LiteDB;
using UnityEngine;

public static class DBHelper
{
    static string DBPath => Path.Combine(Application.persistentDataPath, "mc.db");

    static string LitDB_Connection => $"Filename={DBPath};Connection=shared;";

    public static T With<T>(Func<LiteDatabase, T> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        return f(db);
    }

    public static void With(Action<LiteDatabase> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        f(db);
    }
}