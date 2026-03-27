module GenreExtractor.IO

open Errors
open CCFSharpUtils.Library
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    File.readText' fileInfo |! IoFileReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    File.readLines' fileInfo |! IoFileReadError

let readGenres (genreFile: FileInfo) : Result<string array, Error> =
    if genreFile.Exists then
        readLines genreFile
    else
        Ok Array.empty

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    lines
    |> File.writeLinesToFile filePath
    |! IoFileWriteError

let backupFileIfExists (fileInfo: FileInfo) : Result<string, Error> =
    if fileInfo.Exists then
        fileInfo
        |> File.copyToBackupFile "yyyyMMdd_HHmmss"
        |! IoFileWriteError
        |> Result.map (fun fileInfo -> $"Created backup file \"{fileInfo}\".")
    else
        Ok $"File \"{fileInfo.FullName}\" does not exist, so no back up was done."
