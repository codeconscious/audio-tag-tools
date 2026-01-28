module LibraryAnalysis.IO

open Errors
open Shared.TagLibrary
open Shared.IO
open CCFSharpUtils.Library
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    readFile fileInfo |! IoReadError

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, Error> =
    parseToTags json |! TagParseError
