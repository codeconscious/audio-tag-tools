module DuplicateFinder.ArgValidation

open Errors
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let verifyExists err file =
    if File.Exists file
    then Ok file
    else Error [err]

let validationToResult (v: Validation<'a, string>) : Result<'a, Error> =
    match v with
    | Ok ok ->
        Ok ok
    | Error errs ->
        errs
        |> NonEmptyList.ofList
        |> IoFileMissing
        |> Error

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| settingsFileArg; tagLibArg |] ->
        Validation.map2
            (fun settingsFile tagLib -> FileInfo settingsFile, FileInfo tagLib)
            (settingsFileArg |> verifyExists $"The settings file \"{settingsFileArg}\" does not exist.")
            (tagLibArg       |> verifyExists $"The tag library file \"{tagLibArg}\" does not exist.")
        |> validationToResult
    | _ ->
        Error ArgCountError

