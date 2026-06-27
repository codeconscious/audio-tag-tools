module Cacher.Library

open ArgValidation
open Errors
open IO
open Tags
open Shared.IO
open CCFSharpUtils.IO
open CCFSharpUtils.Operators
open FSharpPlus

let private run args : Result<unit, CommandError> =
    monad {
        let! mediaDir, tagLibFile = validate args
        let! files = getFileInfos mediaDir
        let! tagLibMap = createTagLibMap tagLibFile
        let! newJson = files |> generateJson tagLibMap

        let _ =
            backUpFile tagLibFile
            |-- printfn "Backed up previous file to \"%O\"."
            |!! FileWriteError

        do!
            newJson
            |> File.writeText' tagLibFile
            |-- fun _ -> printfn $"Wrote new file \"%O{tagLibFile}\"."
            |!! FileWriteError
    }

let start args : Result<string, string> =
    match run args with
    | Ok ()   -> Ok "Finished caching successfully."
    | Error e -> Error (message e)
