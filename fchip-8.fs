: bytes ( -- ) ;
\ Take uth byte of u1 from the right
: byte ( u1 u -- u3 ) 8 * rshift $ff and ;
: halfbyte ( u1 u -- u3 ) 4 * rshift $f and ;
: bits ( u -- u' ) 8 / ;
\ Decrement value if it's greather than 0
: dec ( addr -- ) dup c@ dup 0 > if 1- then c! ;

4096 Constant memory-size
16 Constant amount-of-registers
64 32 * bits Constant display-size
16 Constant stack-size

\ Variable to store current opcode
Create opcode 2 allot

\ $000-$1FF - Chip 8 interpreter (contains font set in emu)
\ $050-$0A0 - Used for the built in 4x5 pixel font set (0-F)
\ $200-$FFF - Program ROM and work RAM
Create memory memory-size allot

\ General purpose registers
Create regs amount-of-registers allot

\ Index register
Variable idx
\ Program counter
Variable pc

\ Graphics buffer
Create display display-size allot

\ Two bytes for delay and sound timers
Create timer 2 allot
: delay ( -- addr ) timer ;
: sound ( -- addr ) timer 1 + ;

\ Stack
Create stack stack-size 2 * allot
\ Stack pointer
Variable sp

\ HEX based keypad (0x0-0xF). Use an array to store the current state of the key.
Create keys 16 bytes allot

\ Exceptions codes
: enum ( n -<name>- n+1 ) dup constant 1+ ;
1
enum STACK-UNDERFLOW
enum STACK-OVERFLOW
enum INVALID-REGISTER-NUMBER
drop

\ Initialize graphics
: graphics ( -- ) noop ;

\ Initialize input
: input ( -- ) noop ;

\ Initialize chip-8 state
: initialize ( -- )
  $200 pc ! \ Program starting point
  0 opcode w!
  0 idx !
  0 sp !

  display dislpay-size erase
  stack stack-size 2 * erase
  memory memory-size erase
  regs 16 erase
  0 timer w!

  \ TODO: load fontset
  ;

\ Load chip-8 program
: load ( -- ) noop ;

\ Utilities for opcodes
\ \ Stack
: next ( -- addr ) stack sp @ 2 * + ;
: current ( -- addr ) next 2 - ;
: push ( 2bytes -- )
  sp @ stack-size 1- < if
    next swap w! 1 sp +!
  else
    STACK-OVERFLOW throw
  then ;
: pop ( -- 2bytes )
  sp @ 0 > if
    current w@
    current 0 w! \ Override previous value with zero to enhance security :)
    -1 sp +!
  else
    STACK-UNDERFLOW throw
  then ;
\ \ Register
: reg ( n -- reg-n-addr ) regs swap + ;
: reg-sxy ( regn-x regn-y -- y-value x-value ) reg c@ swap reg c@ ;
\ \ Aux opcodes
: skip-cond ( bool -- ) if 4 pc +! then ;
\ \ Other
\ Skips to next instruction
: skip ( addr -- addr-next ) 2 + ;
: +c! ( n addr -- ) dup >r c@ + r> c! ;

\ Opcodes
\ 0NNN (and 2NNN)
: call ( chip8-addr -- ) pc @ push jump ;
\ 00E0
: clear ( display-addr -- ) display-size erase ;
\ 00EE
: return ( -- ) pop skip jump ;
\ 1NNNx
: jump ( chip8-addr -- ) pc swap ! ;
\ 3XNN
: skip-v-eq ( value regnum ) reg c@ = skip-cond ;
\ 4XNN
: skip-v-neq ( value regnum ) reg c@ <> skip-cond ;
\ 5XY0
: skip-r-eq ( regnum1 regnum2 -- ) regs-xy = skip-cond ;
\ 6XNN
: setv ( value regnum -- ) reg c! ;
\ 7XNN
: set-r-add-v ( value regnum -- ) reg +c! ;
\ 8XY0
: set-r ( regnum1 regnum2 -- ) reg c@ swap reg c! ;
\ 8XY1
: set-r-or-r ( regnum1 regnum2 -- ) over regs-xy or swap reg c! ;
\ 8XY2
: set-r-and-r ( regnum1 regnum2 -- ) over regs-xy and swap reg c! ;
\ 8XY3
: set-r-xor-r ( regnum1 regnum2 -- ) over regs-xy xor swap reg c! ;
\ 8XY4
: set-r-xor-r ( regnum1 regnum2 -- ) ( TODO ) ;

\ Sets opcode to the current progaram counter memory place
: fetch ( addr -- ) w@ opcode w! ;
\ Decodes current opcode
: decode ( -- )
  opcode w@
  dup $F000 and case
    $0000 of
      $000F and case
         $0000 of clear endof
         $000E of return endof
         CR ." Unknown opcode: " hex . decimal
       endcase
    endof
    $1000 of jump endof
    ( TODO: Default case )
  endcase ;

\ Emulate one cycle
: cycle ( -- )
  pc fetch decode

  delay dec
  sound c@ 1 = if ." Beep" then
  sound dec ;

\ Draw graphics on screen
: draw ( -- ) noop ;

: chip-8 ( -- )
  graphics input
  begin noop again ;
