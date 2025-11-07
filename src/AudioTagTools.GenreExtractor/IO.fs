module GenreExtractor.IO

open Errors
open Shared.TagLibrary
open Shared.IO
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    fileInfo
    |> readFile
    |> Result.mapError IoReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    fileInfo
    |> readLines
    |> Result.mapError IoReadError

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, Error> =
    json
    |> parseJsonToTags
    |> Result.mapError TagParseError

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    lines
    |> writeLinesToFile filePath
    |> Result.mapError IoWriteError

let copyToBackupFile fileInfo : Result<FileInfo, Error> =
    fileInfo
    |> copyToBackupFile
    |> Result.mapError IoWriteError
