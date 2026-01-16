module GenreExtractor.IO

open Errors
open Shared.Operators
open Shared.IO
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    fileInfo |> readFile |>! IoReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    fileInfo |> readLines |>! IoReadError

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    lines
    |> writeLinesToFile filePath
    |>! IoWriteError

let copyToBackupFile fileInfo : Result<FileInfo, Error> =
    fileInfo
    |> copyToBackupFile
    |>! IoWriteError
