module DuplicateFinder.ArgValidation

open Errors
open Shared.IO
open CCFSharpUtils.Library
open FSharpPlus
open FSharpPlus.Data
open FsToolkit.ErrorHandling
open System.IO

let validate args : Result<(FileInfo * FileInfo), Error> =
    match args with
    | [| settingsFileArg; tagLibArg |] ->
        applicative {
            let! settingsFile = settingsFileArg |> validateToFileInfo $"Settings file \"{settingsFileArg}\" does not exist."
            and! tagLib = tagLibArg |> validateToFileInfo $"Tag library file \"{tagLibArg}\" does not exist."
            return (settingsFile, tagLib)
        }
        |! (fun errs -> errs |> NonEmptyList.ofList |> IoFilesMissing)
    | _ ->
        Error ArgCountError
