module GenreExtractor.ArgValidation

open Errors
open Shared.IO
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| tagLibArg; genreFileArg |] ->
        applicative {
            let! tagLib = tagLibArg |> validateToFileInfo $"Tag library file \"{tagLibArg}\" does not exist."
            and! genreFile = genreFileArg |> validateToFileInfo $"Genre file \"{genreFileArg}\" does not exist."
            return (tagLib, genreFile)
        }
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError
