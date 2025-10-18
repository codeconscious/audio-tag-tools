module Cacher.ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<DirectoryInfo * FileInfo, Error> =
    match args with
    | [| mediaDirPath; tagLibraryPath |] ->
        Ok (DirectoryInfo mediaDirPath,
            FileInfo tagLibraryPath)
    | _ ->
        Error InvalidArgCount
