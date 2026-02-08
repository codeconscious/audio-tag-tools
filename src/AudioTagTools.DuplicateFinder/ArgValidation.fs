module DuplicateFinder.ArgValidation

open Errors
open Shared.IO
open CCFSharpUtils.Library
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| settingsFileArg; tagLibArg |] ->
        Validation.map2
            (fun settingsFile tagLib -> settingsFile, tagLib)
            (settingsFileArg |> validateToFileInfo $"Settings file \"{settingsFileArg}\" does not exist.")
            (tagLibArg       |> validateToFileInfo $"Tag library file \"{tagLibArg}\" does not exist.")
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError

