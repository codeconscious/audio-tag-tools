[<AutoOpen>]
module Shared.Utilities

open System
open System.Text.Json
open System.Text.Encodings.Web
open System.Text.Unicode
open System.Globalization

/// Format an integer to a comma-separated numeric string. Example: 1000 -> "1,000".
let formatInt (i: int) : string =
    i.ToString("#,##0", CultureInfo.InvariantCulture)

/// Format a 64-bit integer to a comma-separated numeric string. Example: 1000 -> "1,000".
let formatInt64 (i: int64) : string =
    i.ToString("#,##0", CultureInfo.InvariantCulture)

/// Format a float to a comma-separated numeric string. Example: 1000.00 -> "1,000.00".
let formatFloat (f: float) : string =
    f.ToString("#,##0.00", CultureInfo.InvariantCulture)

/// Helper for try/with -> Result.
let private ofTry (f: unit -> 'T) : Result<'T, string> =
    try Ok (f())
    with exn -> Error exn.Message

/// Serialize items to formatted JSON, returning a Result.
/// If an exception is thrown during the underlying operation,
/// the Error only includes its message.
let serializeToJson items : Result<string, string> =
    let options =
        JsonSerializerOptions(
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create UnicodeRanges.All)

    ofTry (fun _ -> JsonSerializer.Serialize(items, options))

/// Reads all text from the specified file, returning a Result.
/// If an exception is thrown during the underlying operation,
/// the Error includes the exception itself.
let readAllText (filePath: string) : Result<string, string> =
    ofTry (fun _ -> System.IO.File.ReadAllText filePath)

/// Removes all instances of multiple substrings from a given string.
let removeSubstrings (substrings: string array) (text: string) : string =
    Array.fold
        (fun acc x -> acc.Replace(x, String.Empty))
        text
        substrings

/// Confirms whether the text of a string exists in any element of nested collections.
let anyContains (collections: string seq seq) (target: string) : bool =
    collections
    |> Seq.concat
    |> Seq.exists (fun text -> StringComparer.InvariantCultureIgnoreCase.Equals(text, target))

/// Formats a TimeSpan to "h:mm:ss" format, where the hours ('h') are optional.
let formatTimeSpan (timeSpan: TimeSpan) : string =
    match timeSpan.Hours with
    | 0 -> sprintf "%dm%ds" timeSpan.Minutes timeSpan.Seconds
    | _ -> sprintf "%dh%dm%ds" timeSpan.Hours timeSpan.Minutes timeSpan.Seconds

let formatBytes (bytes: int64) =
    let kilobyte = 1024L
    let megabyte = kilobyte * 1024L
    let gigabyte = megabyte * 1024L
    let terabyte = gigabyte * 1024L

    match bytes with
    | _ when bytes >= terabyte -> sprintf "%sT" ((float bytes / float terabyte) |> formatFloat)
    | _ when bytes >= gigabyte -> sprintf "%sG" ((float bytes / float gigabyte) |> formatFloat)
    | _ when bytes >= megabyte -> sprintf "%sM" ((float bytes / float megabyte) |> formatFloat)
    | _ when bytes >= kilobyte -> sprintf "%sK" ((float bytes / float kilobyte) |> formatFloat)
    | _ -> sprintf "%s bytes" (bytes |> formatInt64)
