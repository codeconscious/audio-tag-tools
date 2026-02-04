module GenreExtractor.ArgValidation

open Errors
open CCFSharpUtils.Library
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    let verifyExists err file =
        if File.Exists file
        then Ok file
        else Error [err]

    match args with
    | [| tagLibArg; genreFileArg |] ->
        Validation.map2
            (fun settingsFile tagLib -> FileInfo settingsFile, FileInfo tagLib)
            (tagLibArg    |> verifyExists $"Tag library file \"{tagLibArg}\" does not exist.")
            (genreFileArg |> verifyExists $"Genre file \"{genreFileArg}\" does not exist.")
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError
