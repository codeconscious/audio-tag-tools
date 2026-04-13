module GenreExtractor.ArgValidation

open Errors
open CCFSharpUtils
open CCFSharpUtils.Operators
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(FileInfo * FileInfo), CommandError>=
    match args with
    | [| tagLibArg; genreFileArg |] ->
        applicative {
            let! tagLib = tagLibArg |> File.toFileInfoV $"Tag library file \"{tagLibArg}\" does not exist."
            return (tagLib, FileInfo genreFileArg) }
        |> Validation.toResult
        |! (NonEmptyList.ofList >> ArgErrors)
    | _ ->
        Error ArgCountError
