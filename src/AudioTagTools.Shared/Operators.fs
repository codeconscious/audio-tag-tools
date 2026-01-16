[<AutoOpen>]
module Shared.Operators

open FsToolkit.ErrorHandling

/// Operator for Result.mapError.
let inline (|!)
    (r: Result<'ok, 'e1>)
    ([<InlineIfLambda>] f: 'e1 -> 'e2)
    : Result<'ok, 'e2> =
    Result.mapError f r

/// Operator for Result.tee in FsToolkit.ErrorHandling.
let inline (|.)
    (result: Result<'ok, 'error>)
    ([<InlineIfLambda>] sideEffect: 'ok -> unit)
    : Result<'ok, 'error> =
    Result.tee sideEffect result
