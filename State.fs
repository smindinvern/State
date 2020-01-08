namespace smindinvern

module State =

    open Utils

    module Strict =
        /// <summary>
        /// A monad encapsulating a computation and an accompanying state.  The state
        /// is accessible from within the computation to be read and/or updated.
        /// <typeparam name="'a">The type of the value produced by the computation.</typeparam>
        /// <typeparam name="'s">The type of the encapsulated state.</typeparam>
        /// </summary>
        type State<'s, 'a> = 's -> ('s * 'a)
    
        let inline internal bind (f: 'a -> State<'s, 'b>) (m: State<'s, 'a>) (s: 's) =
            let (s', a) = m s
            f a s'
        let inline (>>=) m f = bind f m
        let inline inject (v: 'a) (s: 's) =
            (s, v)
    
        type StateBuilder() =
            member inline __.Bind(m: State<'s, 'a>, f: 'a -> State<'s, 'b>) = bind f m
            member inline __.Return(v: 'a) = inject v
            member inline __.ReturnFrom(m: State<'s, 'a>) = m
    
        let state = new StateBuilder()
    
        /// <summary>
        /// Function application lifted into the State monad.
        /// The lifted function acts on the value produced by the State, rather than the
        /// encapsulated state.
        ///
        /// Just as with normal function application, (<@>) is left-associative.
        /// </summary>
        let inline (<@>) (f: 'a -> 'b) (m: State<'s, 'a>) : State<'s, 'b> =
            bind (inject << f) m
    
        /// <summary>
        /// Function composition lifted into the State monad.
        /// The lifted function acts on the value produced by the State, rather than the
        /// encapsulated state.
        ///
        /// Just as with normal function composition, (<*>) is associative, i.e.
        /// f <*> (g <*> h) = (f <*> g) <*> h
        /// </summary>
        /// <param name="f"></param>
        /// <param name="m"></param>
        let inline (<*>) (f: State<'s, 'a -> 'b>) (m: State<'s, 'a>) : State<'s, 'b> =
            state {
                let! f' = f
                let! m' = m
                return f' m'
            }

        /// <summary>
        /// Return a State computation containing the results of the given list of State computations.
        /// </summary>
        /// <param name="cs"></param>
        let sequence (cs: State<'s, 'a> list) : State<'s, 'a list> =
            List.foldBack (fun t s -> List.cons <@> t <*> s) cs (inject [])
    
        /// <summary>
        /// From within a State computation, get the currently stored state.
        /// </summary>
        let get<'s> : State<'s, 's> =
            fun s -> (s, s)

        /// <summary>
        /// From within a State computation, store a new state.
        /// </summary>
        /// <param name="s">The new state to be stored.</param>
        let put (s: 's) : State<'s, unit> =
            fun _ -> (s, ())

        /// <summary>
        /// From within a State computation, modify the currently stored state.
        /// </summary>
        /// <param name="f">
        /// The currently stored state is run through this function, and the resulting value is stored.
        /// </param>
        let modify (f: 's -> 's) : State<'s, unit> =
            fun s -> (f s, ())
    
        /// <summary>
        /// Given a State computation and an initial state value, run the computation and return the result.
        /// </summary>
        /// <param name="m">The computation to run.</param>
        /// <param name="s">The initial state.</param>
        let inline runState (m: State<'s, 'a>) (s: 's) =
            m s

    module Lazy =
        /// <summary>
        /// A monad encapsulating a computation and an accompanying state.  The state
        /// is accessible from within the computation to be read and/or updated.
        /// <typeparam name="'a">The type of the value produced by the computation.</typeparam>
        /// <typeparam name="'s">The type of the encapsulated state.</typeparam>
        /// </summary>
        type State<'s, 'a> = Lazy<Strict.State<'s, 'a>>

        let inline internal bind (f: 'a -> State<'s, 'b>) (m: State<'s, 'a>) =
            lazy (Strict.bind (fun x -> (f x).Force()) (m.Force()))
        let inline (>>=) m f = bind f m
        let inline inject (v: 'a) =
            lazy (Strict.inject v)

        type StateBuilder() =
            member inline __.Bind(m, f) = bind f m
            member inline __.Return(v) = inject v
            member inline __.ReturnFrom(m) = m

        let state = new StateBuilder()

        /// <summary>
        /// Function application lifted into the State monad.
        /// The lifted function acts on the value produced by the State, rather than the
        /// encapsulated state.
        ///
        /// Just as with normal function application, (<@>) is left-associative.
        /// </summary>
        let inline (<@>) (f: 'a -> 'b) (m: State<'s, 'a>) =
            lazy (Strict.(<@>) f (m.Force()))

        /// <summary>
        /// Function composition lifted into the State monad.
        /// The lifted function acts on the value produced by the State, rather than the
        /// encapsulated state.
        ///
        /// Just as with normal function composition, (<*>) is associative, i.e.
        /// f <*> (g <*> h) = (f <*> g) <*> h
        /// </summary>
        /// <param name="f"></param>
        /// <param name="m"></param>
        let inline (<*>) (f: State<'s, 'a -> 'b>) (m: State<'s, 'a>) =
            lazy (Strict.(<*>) (f.Force()) (m.Force()))

        /// <summary>
        /// Return a State computation containing the results of the given list of State computations.
        /// </summary>
        /// <param name="cs"></param>
        let sequence (cs: State<'s, 'a> list) : State<'s, 'a list> =
            List.foldBack (fun t s -> List.cons <@> t <*> s) cs (inject [])

        /// <summary>
        /// From within a State computation, get the currently stored state.
        /// </summary>
        let get<'s> = lazy Strict.get<'s>

        /// <summary>
        /// From within a State computation, store a new state.
        /// </summary>
        /// <param name="s">The new state to be stored.</param>
        let put s = lazy (Strict.put s)

        /// <summary>
        /// From within a State computation, modify the currently stored state.
        /// </summary>
        /// <param name="f">
        /// The currently stored state is run through this function, and the resulting value is stored.
        /// </param>
        let modify f = lazy (Strict.modify f)

        /// <summary>
        /// Given a State computation and an initial state value, run the computation and return the result.
        /// </summary>
        /// <param name="m">The computation to run.</param>
        /// <param name="s">The initial state.</param>
        let inline runState (m: State<'s, 'a>) s =
            Strict.runState (m.Force()) s
