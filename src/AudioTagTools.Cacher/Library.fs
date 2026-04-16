module Cacher.Library

open ArgValidation
open Errors
open IO
open Tags
open Shared.Constants
open CCFSharpUtils
open CCFSharpUtils.Operators
open FSharpPlus

let private run args : Result<unit, CommandError> =
    monad' {
        let! mediaDir, tagLibraryFile = validate args
        let! fileInfos = getFileInfos mediaDir
        let! tagLibraryMap = createTagLibraryMap tagLibraryFile
        let! newJson = fileInfos |> generateJson tagLibraryMap

        let _ =
            backUpFile tagLibraryFile
            |- printfn "Backed up previous file to \"%O\"."
            |!! FileWriteError

        do!
            newJson
            |> File.writeText' tagLibraryFile
            |- fun _ -> printfn $"Wrote new file \"%O{tagLibraryFile}\"."
            |!! FileWriteError
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished caching successfully."
    | Error e -> Error (message e)
