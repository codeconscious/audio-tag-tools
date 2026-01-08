[<AutoOpen>]
module Shared.Common

open System
open System.IO
open System.Text
open System.Text.Json
open System.Text.Encodings.Web
open System.Text.Unicode
open System.Globalization

/// Helper for try/with -> Result.
let ofTry (f: unit -> 'a) : Result<'a, string> =
    try Ok (f())
    with exn -> Error exn.Message

module Numerics =

    let inline isZero (n: ^a) =
        n = LanguagePrimitives.GenericZero<'a>

    let inline isOne (n: ^a) =
        n = LanguagePrimitives.GenericOne<'a>

    /// Formats a number of any type to a comma-formatted string.
    let inline formatNumber (i: ^T) : string
        when ^T : (member ToString : string * IFormatProvider -> string) =
        (^T : (member ToString : string * IFormatProvider -> string) (i, "#,##0", CultureInfo.InvariantCulture))

[<RequireQualifiedAccess>]
module String =

    let newLine = Environment.NewLine

    let hasNoText text =
        String.IsNullOrWhiteSpace text

    let hasText text =
        not (hasNoText text)

    let allHaveText xs =
        xs |> List.forall hasText

    let textOrFallback fallback text =
        if hasText text then text else fallback

    let textOrEmpty text =
        textOrFallback text String.Empty

    let equalIgnoringCase x y =
        String.Equals(x, y, StringComparison.OrdinalIgnoreCase)

    let startsWithIgnoreCase startText (text: string)  =
        text.StartsWith(startText, StringComparison.InvariantCultureIgnoreCase)

    let endsWithIgnoreCase endText (text: string) =
        text.EndsWith(endText, StringComparison.InvariantCultureIgnoreCase)

    /// Pluralize text using a specified count.
    let inline pluralize ifOne ifNotOne count =
        if Numerics.isOne count then ifOne else ifNotOne

    /// Pluralize text including its count, such as "1 file", "30 URLs".
    let inline pluralizeWithCount ifOne ifNotOne count =
        sprintf "%d %s" count (pluralize ifOne ifNotOne count)

    let inline private fileLabeller descriptor (count: int) =
        match descriptor with
        | None   -> $"""%s{Numerics.formatNumber count} %s{pluralize "file" "files" count}"""
        | Some d -> $"""%s{Numerics.formatNumber count} %s{d} {pluralize "file" "files" count}"""

    /// Returns a file-count string, such as "0 files" or 1 file" or "140 files".
    let fileLabel count =
        fileLabeller None count

    /// Returns a file-count string with a descriptor, such as "0 audio files" or "140 deleted files".
    let fileLabelWithDescriptor (descriptor: string) count =
        fileLabeller (Some (descriptor.Trim())) count

    /// Returns a new string in which all invalid path characters for the current OS
    /// have been replaced by the specified replacement character.
    /// Throws if the replacement character is an invalid path character.
    let replaceInvalidPathChars
        (replaceWith: char option)
        (customInvalidChars: char list option)
        (text: string)
        : string =

        let replaceWith = defaultArg replaceWith '_'
        let custom = defaultArg customInvalidChars []

        let invalidChars =
            seq {
                yield! Path.GetInvalidFileNameChars()
                yield! Path.GetInvalidPathChars()
                yield  Path.PathSeparator
                yield  Path.DirectorySeparatorChar
                yield  Path.AltDirectorySeparatorChar
                yield  Path.VolumeSeparatorChar
                yield! custom
            }
            |> Set.ofSeq

        if invalidChars |> Set.contains replaceWith  then
            invalidArg "replaceWith" $"The replacement char ('%c{replaceWith}') must be a valid path character."

        Set.fold
            (fun (sb: StringBuilder) ch -> sb.Replace(ch, replaceWith))
            (StringBuilder text)
            invalidChars
        |> _.ToString()

    let trimTerminalLineBreak (text: string) =
        text.TrimEnd(newLine.ToCharArray())

    /// Formats an integer to a comma-separated numeric string. Example: 1000 -> "1,000".
    let formatInt (i: int) : string =
        i.ToString("#,##0", CultureInfo.InvariantCulture)

    /// Formats a 64-bit integer to a comma-separated numeric string. Example: 1000 -> "1,000".
    let formatInt64 (i: int64) : string =
        i.ToString("#,##0", CultureInfo.InvariantCulture)

    /// Formats a float to a comma-separated numeric string. Example: 1000.00 -> "1,000.00".
    let formatFloat (f: float) : string =
        f.ToString("#,##0.00", CultureInfo.InvariantCulture)

    /// Formats a float to a percentage string with a custom number of decimal places.
    let formatPercent (decimalPlaces: int) (n: float) : string =
        let decimalPlaces' = if decimalPlaces < 0 then 0 else decimalPlaces
        let pct = n * 100.0
        pct.ToString("F" + decimalPlaces'.ToString(), CultureInfo.InvariantCulture) + "%"

    /// Formats a byte count into a human-friendly size representation using KB, MB, GB, or TB.
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

    /// Formats a TimeSpan to "h:mm:ss" format, where the hours ('h') are optional.
    let formatTimeSpan (timeSpan: TimeSpan) : string =
        match timeSpan.Hours with
        | 0 -> sprintf "%dm%ds" timeSpan.Minutes timeSpan.Seconds
        | _ -> sprintf "%dh%dm%ds" timeSpan.Hours timeSpan.Minutes timeSpan.Seconds

    /// Serializes items to a formatted JSON string, returning a Result.
    /// If an exception is thrown during the underlying operation,
    /// the Error only includes its message.
    let serializeToJson items : Result<string, string> =
        let options =
            JsonSerializerOptions(
                WriteIndented = true,
                Encoder = JavaScriptEncoder.Create UnicodeRanges.All)

        ofTry (fun _ -> JsonSerializer.Serialize(items, options))

    /// Removes all instances of multiple substrings from a given string.
    let stripSubstrings (substrings: string array) (text: string) : string =
        Array.fold
            (fun acc x -> acc.Replace(x, String.Empty, StringComparison.InvariantCultureIgnoreCase))
            text
            substrings

    let whiteSpaces =
        [|
            "\u0020" // space
            "\u00A0" // non-breaking space
            "\u1680" // Ogham space mark
            "\u180E" // Mongolian vowel separator
            "\u2000" // en quad
            "\u2001" // em quad
            "\u2002" // en space
            "\u2003" // em space
            "\u2004" // three-per-em space
            "\u2005" // four-per-em space
            "\u2006" // six-per-em space
            "\u2007" // figure space
            "\u2008" // punctuation space
            "\u2009" // thin space
            "\u200A" // hair space
            "\u200B" // zero-width space
            "\u200D" // zero-width joiner (emoji)
            "\u202F" // narrow non-breaking space
            "\u205F" // medium mathematical space
            "\u2063" // invisible separator
            "\u3000" // ideographic space (i.e., Japanese full-width space)
            "\u3164" // Hangul filler
            "\uFEFF" // zero-width non-breaking space
        |]

    let stripPunctuation (text: string) : string =
        text.ToCharArray()
        |> Array.filter (not << Char.IsPunctuation)
        |> String

[<RequireQualifiedAccess>]
module Seq =

    let isNotEmpty seq = not (Seq.isEmpty seq)

    let doesNotContain x seq = not <| Seq.contains x seq

    let hasOne seq = seq |> Seq.length |> Numerics.isOne

    let hasMultiple seq = seq |> Seq.length |> (<) 1

    let caseInsensitiveContains text (xs: string seq) : bool =
        xs |> Seq.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

    /// Confirms whether the text of a string exists in any element of nested collections.
    let anyContains (collections: string seq seq) (target: string) : bool =
        collections
        |> Seq.concat
        |> Seq.exists (fun text -> StringComparer.InvariantCultureIgnoreCase.Equals(text, target))

[<RequireQualifiedAccess>]
module List =

    let isNotEmpty lst = not (List.isEmpty lst)

    let doesNotContain x lst = not <| List.contains x lst

    let hasOne lst = lst |> List.length |> Numerics.isOne

    let hasMultiple lst = lst |> List.length |> (<) 1

    let caseInsensitiveContains text (lst: string list) : bool =
        lst |> List.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

[<RequireQualifiedAccess>]
module Array =

    let isNotEmpty arr = not <| Array.isEmpty arr

    let doesNotContain x arr = not <| Array.contains x arr

    let hasOne arr = arr |> Array.length |> Numerics.isOne

    let hasMultiple arr = arr |> Array.length |> (<) 1

    let caseInsensitiveContains text (arr: string array) : bool =
        arr |> Array.exists (fun x -> String.Equals(x, text, StringComparison.OrdinalIgnoreCase))

    /// If the array is empty, returns None. Otherwise, wraps the array in Some.
    let toOption arr = if Array.isEmpty arr then None else Some arr
