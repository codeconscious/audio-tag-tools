module LibraryAnalysis.IO

open Errors
open Shared.TagLibrary
open Shared.IO
open System.IO
open FsToolkit.ErrorHandling

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    readFile fileInfo |> Result.mapError IoReadError

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, Error> =
    parseToTags json |> Result.mapError TagParseError
