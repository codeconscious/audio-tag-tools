[<AutoOpen>]
module Shared.Operators

open FsToolkit.ErrorHandling

let inline (<.>)
    ([<InlineIfLambda>] sideEffect: 'ok -> unit)
    (result: Result<'ok, 'error>)
    : Result<'ok, 'error> =
    Result.tee sideEffect result

let inline (<&>)
    ([<InlineIfLambda>] action: 'T -> unit)
    (result: Result<'T, 'Error>)
    : unit =
    Result.iter action result
