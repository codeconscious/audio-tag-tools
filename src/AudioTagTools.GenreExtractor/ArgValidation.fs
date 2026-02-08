module GenreExtractor.ArgValidation

open Errors
open Shared.IO
open CCFSharpUtils.Library
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| tagLibArg; genreFileArg |] ->
        Validation.map2
            (fun settingsFile tagLib -> settingsFile, tagLib)
            (tagLibArg    |> validateToFileInfo $"Tag library file \"{tagLibArg}\" does not exist.")
            (genreFileArg |> validateToFileInfo $"Genre file \"{genreFileArg}\" does not exist.")
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError
