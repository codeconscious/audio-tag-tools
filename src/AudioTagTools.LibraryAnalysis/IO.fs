module LibraryAnalysis.IO

open Errors
open Shared.TagLibrary
open CCFSharpUtils.Library
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    File.readText' fileInfo |! IoReadError

let parseJsonToTags (json: string) : Result<MultipleLibraryTags, Error> =
    parseToTags json |! TagParseError
