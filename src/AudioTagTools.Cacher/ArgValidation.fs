module Cacher.ArgValidation

open Errors
open Shared.IO
open FSharpPlus
open FSharpPlus.Data
open System.IO

let validate args : Result<(DirectoryInfo * FileInfo), Error> =
    match args with
    | [| mediaDirArg; tagLibArg |] ->
        applicative {
            let! mediaDir = mediaDirArg |> validateToDirInfo (MediaDirectoryMissing mediaDirArg)
            return (mediaDir, FileInfo tagLibArg)
        }
        |> Validation.toResult
    | _ ->
        Error InvalidArgCount
