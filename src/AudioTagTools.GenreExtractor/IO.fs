module GenreExtractor.IO

open Errors
open FSharpPlus
open CCFSharpUtils
open CCFSharpUtils.Operators
open System.IO

let readGenres (genreFile: FileInfo) : Result<string list, CommandError> =
    if genreFile.Exists then
        genreFile
        |> File.readLines'
        |>> List.ofArray
        |!! FileReadError
    else
        Ok List.empty
