module GenreExtractor.IO

open Errors
open CCFSharpUtils.Library
open System.IO

let readGenres (genreFile: FileInfo) : Result<string array, Error> =
    if genreFile.Exists then
        genreFile |> File.readLines' |! FileReadError
    else
        Ok Array.empty

