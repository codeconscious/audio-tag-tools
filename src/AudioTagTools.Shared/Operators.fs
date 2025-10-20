[<AutoOpen>]
module Shared.Operators

open FsToolkit.ErrorHandling

let (>>=) result func = Result.bind func result
let (<!>) result func = Result.map func result
let (<.>) result func = Result.tee func result
let (<&>) result func = Result.iter func result
