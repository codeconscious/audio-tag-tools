module Utilities

open System
open System.Text.Json
open System.Text.Encodings.Web
open System.Text.Unicode
open System.Globalization

/// Helper for try/with -> Result.
let private ofTry (f: unit -> 'T) : Result<'T, string> =
    try    Ok (f())
    with e -> Error e.Message

/// Serialize items to formatted JSON, returning a Result.
/// If an exception is thrown during the underlying operation,
/// the Error only includes its message.
let serializeToJson items : Result<string, string> =
    let serializerOptions =
        JsonSerializerOptions(
            WriteIndented = true,
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All))

    ofTry (fun _ -> JsonSerializer.Serialize(items, serializerOptions))

/// Reads all text from the specified file, returning a Result.
/// If an exception is thrown during the underlying operation,
/// the Error includes the exception itself.
let readAllText (filePath: string) : Result<string, string> =
    ofTry (fun _ -> System.IO.File.ReadAllText filePath)

/// Format an integer to a comma-separated numeric string. Example: 1000 -> "1,000".
let formatInt (i: int) : string =
    i.ToString("#,##0", CultureInfo.InvariantCulture)

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


