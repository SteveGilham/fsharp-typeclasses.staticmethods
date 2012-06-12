﻿module Prelude

let flip f x y = f y x
let const' k _ = k
let maybe  n f = function | None -> n | Some x -> f x

type Ordering = LT|EQ|GT

let inline compare' x y =
    match compare x y with
    | a when a > 0 -> GT
    | a when a < 0 -> LT
    | _            -> EQ

type Either<'a,'b> = Left of 'a | Right of 'b
let either f g = function Left x -> f x | Right y -> g y

// IO ---------------------------------------------------------------------

type IO<'a> = IO of (unit->'a)
let runIO  (IO f)   = f()
let primretIO  f    = IO(fun () -> f)
let primbindIO io f = IO(fun () -> runIO (f (runIO io )))

let getLine    = IO(fun() -> System.Console.ReadLine())
let putStrLn x = IO(fun() -> printfn "%s" x)
let print    x = IO(fun() -> printfn "%A" x)


// Functor class ----------------------------------------------------------
#nowarn "64"
type Functor = Functor with
    static member fmap (Functor, x:option<_>    ) = fun f -> Option.map f x
    static member fmap (Functor, x:list<_>      ) = fun f -> List.map   f x
    static member fmap (Functor, x:IO<_>        ) = fun f -> primbindIO x (primretIO << f)
    static member fmap (Functor, g:_->_         ) = (>>) g
    static member fmap (Functor, e:Either<'a,'b>) = fun f ->
        match e with
        | (Left x ) -> Left x
        | (Right y) -> Right (f y)

let inline fmap f x =   ((^C or ^a) : (static member fmap : ^C * ^a -> _) (Functor, x)) f


// Monad class ------------------------------------------------------------

type Monad = Monad with
    static member return' (Monad, _ : 'a option  ) = fun (x:'a) -> Some x
    static member return' (Monad, _ : 'a list    ) = fun (x:'a) -> [x]
    static member return' (Monad, _ :'a IO       ) = fun (x:'a) -> primretIO x
    static member return' (Monad, _ : _ -> 'a    ) = fun (x:'a) -> const' x
    static member return' (Monad, _ :Either<_,'a>) = fun (x:'a) -> Right x
     
    static member bind (Monad,x:'a option               ) = fun f -> Option.bind f x  
    static member bind (Monad,x:option<_>  ,_:option<'b>) = fun f -> Option.bind f x
    static member bind (Monad,x:list<_>    ,_:list<'b>  ) = fun f -> 
                                                                let rec bnd f = function
                                                                                | x::xs -> f x @ bnd f xs
                                                                                | []    -> []
                                                                bnd f x
    static member bind (Monad,x:IO<_>        ,_:IO<'b>       ) = fun f -> primbindIO x f
    static member bind (Monad,f:'e->'a       ,_:'e->'b       ) = fun (k:'a->'e->'b) r -> k (f r) r
    static member bind (Monad,x:Either<'e,'a>,_:Either<'e,'b>) = fun (k:_->Either<_,'b>) -> match x with
                                                                                                    | Left  l -> Left l
                                                                                                    | Right r -> k r

let inline return' x : ^R = ((^C or ^R) : (static member return' : ^C * ^R   -> _) (Monad, Unchecked.defaultof< ^R>)   ) x
let inline (>>=) x f : ^R = ((^C or ^a or ^R) : (static member bind : ^C * ^a * ^R -> _) (Monad, x, Unchecked.defaultof< ^R>)) f

// Do notation ------------------------------------------------------------

type DoNotationBuilder() =
    member inline b.Return(x)    = return' x
    member inline b.Bind(p,rest) = p >>= rest
    member        b.Let (p,rest) = rest p
    member    b.ReturnFrom(expr) = expr
let do' = new DoNotationBuilder()