module IO

open System.IO
open Errors
open TagLibrary
open AudioTagTools.Shared.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    readFile fileInfo
    |> Result.mapError IoReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    readLines fileInfo
    |> Result.mapError IoReadError

let parseJsonToTags (json: string) : Result<LibraryTags array, Error> =
    parseJsonToTags json
    |> Result.mapError TagParseError

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    writeLinesToFile filePath lines
    |> Result.mapError IoWriteError
