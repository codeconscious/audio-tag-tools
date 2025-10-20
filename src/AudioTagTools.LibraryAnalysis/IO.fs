module LibraryAnalysis.IO

open Errors
open Shared.TagLibrary
open Shared.IO
open System.IO
open FsToolkit.ErrorHandling

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    fileInfo
    |> readFile
    |> Result.mapError IoReadError


let parseJsonToTags (json: string) : Result<LibraryTags array, Error> =
    json
    |> parseJsonToTags
    |> Result.mapError TagParseError
