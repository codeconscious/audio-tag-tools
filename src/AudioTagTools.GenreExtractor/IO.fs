module GenreExtractor.IO

open Errors
open Shared.IO
open CCFSharpUtils.Library
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    readFile fileInfo |! IoFileReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    readLines fileInfo |! IoFileReadError

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    lines
    |> writeLinesToFile filePath
    |! IoFileWriteError

let copyToBackupFile fileInfo : Result<FileInfo, Error> =
    fileInfo
    |> copyToBackupFile
    |! IoFileWriteError
