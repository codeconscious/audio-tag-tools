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

let backupFileIfExists (fileInfo: FileInfo) : Result<string, Error> =
    if fileInfo.Exists then
        fileInfo
        |> copyToBackupFile
        |! IoFileWriteError
        |> Result.map (fun fileInfo -> $"Created backup file \"{fileInfo}\".")
    else
        Ok $"File \"{fileInfo.FullName}\" does not exist, so no back up was done."
