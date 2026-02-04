module DuplicateFinder.ArgValidation

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
    | [| settingsFileArg; tagLibArg |] ->
        Validation.map2
            (fun settingsFile tagLib -> FileInfo settingsFile, FileInfo tagLib)
            (settingsFileArg |> verifyExists $"Settings file \"{settingsFileArg}\" does not exist.")
            (tagLibArg       |> verifyExists $"Tag library file \"{tagLibArg}\" does not exist.")
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError

