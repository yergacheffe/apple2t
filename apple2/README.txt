TwitterII.m65 is the 6502 assembly source for the code that runs on the
Apple II. I used the freely-available ATAsm assembler to assemble it. The
binary file is in TwitterII.bin and must be loaded at $6000 on the Apple II
in order to work.

There is a bootstrapper binary here as well that you can use to transfer
the full TwitterII client from your PC. You will need to hand-enter the
bootstrap program by entering the commands from bootstrap.txt into the Apple II.
If you have a serial card you can also pipe these from a PC through serial,
but I haven't tried this myself.

1. If you're at a BASIC prompt (> or ]) enter CALL -151 to enter the monitor.
2. You should be looking at the monitor prompt "*".
3. Enter the lines from bootstrap.txt exactly as they are displayed:

	300:AD 63 C0 10 FB A9 00 8D 
	:16 03 20 20 03 8D 17 03 
	:20 20 03 A0 00 99 34 12 
	:EE 16 03 D0 F3 4C 00 03 
	:A9 00 A2 08 AC 62 C0 10 
	:FB 0A AC 61 C0 10 02 09 
	:01 AC 62 C0 30 FB CA D0 
	:EB 60

4. Once you've entered the code, execute it by typing 300G
5. On the .Net program on your PC uncomment the line that reads "apple2.LoadCode();"
   and execute that line of code. This should run for a short while and then return.
6. Validate that you have the code loaded at $6000 by entering 6000L in the monitor.
   The disassembled code should look like the code in TwitterII.m65. You should see
   JSR $XXXX, JMP $XXXX, LDA $XXXX etc. and not a bunch of question marks.
7. You'll want to save this to a cassette or floppy. Save from addresses $6000..$63FF
   which is actually more than needed but will work if the code grows to 1K in the
   future.
8. Finally run the code by typing 6000G at the monitor prompt. At this point you can
   run the PC twitter client code and should start seeing tweets appear!
   
