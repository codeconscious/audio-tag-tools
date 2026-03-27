module GenreExtractor.IO

open Errors
open Shared.Constants
open CCFSharpUtils.Library
open System.IO

let readFile (fileInfo: FileInfo) : Result<string, Error> =
    File.readText' fileInfo |! FileReadError

let readLines (fileInfo: FileInfo) : Result<string array, Error> =
    File.readLines' fileInfo |! FileReadError

let readGenres (genreFile: FileInfo) : Result<string array, Error> =
    if genreFile.Exists then
        readLines genreFile
    else
        Ok Array.empty

let writeLines (filePath: string) (lines: string array) : Result<unit, Error> =
    lines
    |> File.writeLinesToFile filePath
    |! FileWriteError
