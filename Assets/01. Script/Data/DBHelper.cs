using System;
using System.IO;
using LiteDB;
using UnityEngine;

/// <summary>
/// DBHelper - LiteDB 데이터베이스 파일(mc.db) 연결을 관리하는 정적 유틸리티
///
/// 【역할】 LiteDB 커넥션 문자열을 생성하고, using 패턴으로 안전하게 DB 연결을 관리한다.
///          Application.persistentDataPath에 mc.db 파일을 생성/사용한다.
///          Connection=shared 모드로 동시 접근을 허용한다.
/// 【참조하는 곳】 DBGateway.WithDb() (유일한 호출자)
/// 【참조되는 곳】 LiteDB 라이브러리
/// 【흐름】 DBGateway.WithDb() → DBHelper.With() → new LiteDatabase(연결문자열) → 콜백 실행 → Dispose
/// </summary>
public static class DBHelper
{
    /// <summary>DB 파일 경로. Application.persistentDataPath/mc.db (배포 시 이 경로 확인 필요)</summary>
    static string DBPath => Path.Combine(Application.persistentDataPath, "mc.db");

    /// <summary>LiteDB 연결 문자열. shared 모드로 여러 곳에서 동시 접근 가능</summary>
    static string LitDB_Connection => $"Filename={DBPath};Connection=shared;";

    /// <summary>
    /// DB 커넥션을 열고 함수를 실행한 후 결과를 반환한다.
    /// using 블록으로 커넥션이 자동 해제되어 리소스 누수를 방지한다.
    /// persistentDataPath 디렉토리가 없으면 자동 생성한다.
    /// </summary>
    public static T With<T>(Func<LiteDatabase, T> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        return f(db);
    }

    /// <summary>
    /// DB 커넥션을 열고 액션을 실행한다. 반환값이 없는 쓰기 작업(Insert, Update, Delete)용.
    /// </summary>
    public static void With(Action<LiteDatabase> f)
    {
        Directory.CreateDirectory(Application.persistentDataPath);
        using var db = new LiteDatabase(LitDB_Connection);
        f(db);
    }
}