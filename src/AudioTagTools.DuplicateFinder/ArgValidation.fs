module DuplicateFinder.ArgValidation

open Errors
open System.IO

let validate (args: string array) : Result<FileInfo * FileInfo, Error> =
    match args with
    | [| settingsFilePath; tagLibraryPath |] ->
        Ok (FileInfo settingsFilePath,
            FileInfo tagLibraryPath)
    | _ ->
        Error InvalidArgCount
