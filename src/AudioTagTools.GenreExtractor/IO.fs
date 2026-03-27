module GenreExtractor.IO

open Errors
open Shared.Constants
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
        |> File.copyToBackupFile timeStampFormat
        |* sprintf "Created backup file \"%O\"." // %O formats any object via ToString().
        |! IoFileWriteError
    else
        Ok $"File \"{fileInfo.FullName}\" does not exist, so no back up was done."
