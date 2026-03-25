module GenreExtractor.ArgValidation

open Errors
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error>=
    match args with
    | [| tagLibArg; genreFileArg |] ->
        applicative {
            let! tagLib = tagLibArg |> File.toFileInfo $"Tag library file \"{tagLibArg}\" does not exist."
            return (tagLib, FileInfo genreFileArg)
        }
        |> Validation.toResult
        |> Result.mapError (NonEmptyList.ofList >> ArgErrors)
    | _ ->
        Error ArgCountError
