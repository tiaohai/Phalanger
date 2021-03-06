namespace PHP.Core.Parsers
{
	#region User Code
	
	/*
 Copyright (c) 2004-2006 Tomas Matousek. Based on PHP5 and PHP6 grammar tokens definition. 
 The use and distribution terms for this software are contained in the file named License.txt, 
 which can be found in the root of the Phalanger distribution. By using this software 
 in any fashion, you are agreeing to be bound by the terms of this license.
 You must not remove this notice from this software.
*/
using System;
using PHP.Core;
using System.Collections.Generic;
#endregion
	
	
	public partial class Lexer
	{
		public enum LexicalStates
		{
			INITIAL = 0,
			ST_IN_SCRIPTING = 1,
			ST_DOUBLE_QUOTES = 2,
			ST_SINGLE_QUOTES = 3,
			ST_BACKQUOTE = 4,
			ST_HEREDOC = 5,
			ST_LOOKING_FOR_PROPERTY = 6,
			ST_LOOKING_FOR_VARNAME = 7,
			ST_DOC_COMMENT = 8,
			ST_COMMENT = 9,
			ST_ONE_LINE_COMMENT = 10,
		}
		
		[Flags]
		private enum AcceptConditions : byte
		{
			NotAccept = 0,
			AcceptOnStart = 1,
			AcceptOnEnd = 2,
			Accept = 4
		}
		
		public struct Position
		{
			public int Char;
			public int Line;
			public int Column;
			public Position(int ch, int line, int column)
			{
				this.Char = ch;
				this.Line = line;
				this.Column = column;
			}
		}
		private const int NoState = -1;
		private const char BOL = (char)128;
		private const char EOF = (char)129;
		
		private Tokens yyreturn;
		
		private System.IO.TextReader reader;
		private char[] buffer = new char[512];
		
		// whether the currently parsed token is being expanded (yymore has been called):
		private bool expanding_token;
		
		// offset in buffer where the currently parsed token starts:
		private int token_start;
		
		// offset in buffer where the currently parsed token chunk starts:
		private int token_chunk_start;
		
		// offset in buffer one char behind the currently parsed token (chunk) ending character:
		private int token_end;
		
		// offset of the lookahead character (number of characters parsed):
		private int lookahead_index;
		
		// number of characters read into the buffer:
		private int chars_read;
		
		// parsed token start position (wrt beginning of the stream):
		protected Position token_start_pos;
		
		// parsed token end position (wrt beginning of the stream):
		protected Position token_end_pos;
		
		private bool yy_at_bol = false;
		
		public LexicalStates CurrentLexicalState { get { return current_lexical_state; } set { current_lexical_state = value; } } 
		private LexicalStates current_lexical_state;
		
		public Lexer(System.IO.TextReader reader)
		{
			Initialize(reader, LexicalStates.INITIAL);
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState, bool atBol)
		{
			this.expanding_token = false;
			this.token_start = 0;
			this.chars_read = 0;
			this.lookahead_index = 0;
			this.token_chunk_start = 0;
			this.token_end = 0;
			this.reader = reader;
			this.yy_at_bol = atBol;
			this.current_lexical_state = lexicalState;
		}
		
		public void Initialize(System.IO.TextReader reader, LexicalStates lexicalState)
		{
			Initialize(reader, lexicalState, false);
		}
		
		#region Accept
		
		#pragma warning disable 162
		
		
		Tokens Accept0(int state,out bool accepted)
		{
			accepted = true;
			
			switch(state)
			{
				case 2:
					// #line 75
					{ 
						return Tokens.T_INLINE_HTML; 
					}
					break;
					
				case 3:
					// #line 103
					{ 
						if (AllowAspTags)
						{
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 4:
					// #line 79
					{
						if (AllowShortTags || TokenLength>2) 
						{ 
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 5:
					// #line 91
					{
						if (GetTokenChar(1) == '%' && AllowAspTags || GetTokenChar(1) == '?' && AllowShortTags) 
						{
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_OPEN_TAG_WITH_ECHO;
						} 
						else 
						{
							return Tokens.T_INLINE_HTML;
						}
					}
					break;
					
				case 6:
					// #line 115
					{
						BEGIN(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_OPEN_TAG;
					}
					break;
					
				case 7:
					// #line 273
					{ return (Tokens)GetTokenChar(0); }
					break;
					
				case 8:
					// #line 348
					{ 
						BEGIN(LexicalStates.ST_BACKQUOTE); 
						return Tokens.T_BACKQUOTE; 
					}
					break;
					
				case 9:
					// #line 274
					{ return Tokens.T_STRING; }
					break;
					
				case 10:
					// #line 276
					{ return Tokens.T_WHITESPACE; }
					break;
					
				case 11:
					// #line 332
					{ 
						BEGIN(LexicalStates.ST_DOUBLE_QUOTES); 
						return (GetTokenChar(0) != '"') ? Tokens.T_BINARY_DOUBLE : Tokens.T_DOUBLE_QUOTES; 
					}
					break;
					
				case 12:
					// #line 338
					{ 
						// Gets here only in the case of unterminated singly-quoted string. That leads usually to an error token,
						// however when the source code is parsed per-line (as in Visual Studio colorizer) it is important to remember
						// that we are in the singly-quoted string at the end of the line.
						BEGIN(LexicalStates.ST_SINGLE_QUOTES); 
						yymore(); 
						break; 
					}
					break;
					
				case 13:
					// #line 277
					{ return Tokens.ParseDecimalNumber; }
					break;
					
				case 14:
					// #line 275
					{ return Tokens.T_NS_SEPARATOR; }
					break;
					
				case 15:
					// #line 287
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 16:
					// #line 310
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LBRACE; }
					break;
					
				case 17:
					// #line 360
					{ return Tokens.ERROR; }
					break;
					
				case 18:
					// #line 311
					{ if (!yy_pop_state()) return Tokens.ERROR; return Tokens.T_RBRACE; }
					break;
					
				case 19:
					// #line 258
					{ return Tokens.T_MOD_EQUAL; }
					break;
					
				case 20:
					// #line 313
					{
						if (AllowAspTags) 
						{
							BEGIN(LexicalStates.INITIAL);
							return Tokens.T_CLOSE_TAG;
						} 
						else
						{
							yyless(1);
							return Tokens.T_PERCENT;
						}
					}
					break;
					
				case 21:
					// #line 266
					{ return Tokens.T_SL; }
					break;
					
				case 22:
					// #line 251
					{ return Tokens.T_IS_SMALLER_OR_EQUAL; }
					break;
					
				case 23:
					// #line 250
					{ return Tokens.T_IS_NOT_EQUAL; }
					break;
					
				case 24:
					// #line 224
					{ return Tokens.T_LGENERIC; }
					break;
					
				case 25:
					// #line 123
					{ 
						BEGIN(LexicalStates.INITIAL); 
						return Tokens.T_CLOSE_TAG; 
					}
					break;
					
				case 26:
					// #line 219
					{ return (InLinq) ? Tokens.T_LINQ_IN : Tokens.T_STRING; }
					break;
					
				case 27:
					// #line 137
					{ return Tokens.T_IF; }
					break;
					
				case 28:
					// #line 148
					{ return Tokens.T_AS; }
					break;
					
				case 29:
					// #line 249
					{ return Tokens.T_IS_EQUAL; }
					break;
					
				case 30:
					// #line 244
					{ return Tokens.T_DOUBLE_ARROW; }
					break;
					
				case 31:
					// #line 326
					{ return Tokens.DoubleQuotedString; }
					break;
					
				case 32:
					// #line 327
					{ return Tokens.SingleQuotedString; }
					break;
					
				case 33:
					// #line 252
					{ return Tokens.T_IS_GREATER_OR_EQUAL; }
					break;
					
				case 34:
					// #line 267
					{ return Tokens.T_SR; }
					break;
					
				case 35:
					// #line 256
					{ return Tokens.T_DIV_EQUAL; }
					break;
					
				case 36:
					// #line 288
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); yymore(); break; }
					break;
					
				case 37:
					// #line 290
					{ BEGIN(LexicalStates.ST_COMMENT); yymore(); break; }
					break;
					
				case 38:
					// #line 143
					{ return Tokens.T_DO; }
					break;
					
				case 39:
					// #line 171
					{ return Tokens.T_LOGICAL_OR; }
					break;
					
				case 40:
					// #line 220
					{ return (InLinq) ? Tokens.T_LINQ_BY : Tokens.T_STRING; }
					break;
					
				case 41:
					// #line 279
					{ return Tokens.ParseDouble; }
					break;
					
				case 42:
					// #line 225
					{ return Tokens.T_RGENERIC; }
					break;
					
				case 43:
					// #line 268
					{ return Tokens.T_DOUBLE_COLON; }
					break;
					
				case 44:
					// #line 253
					{ return Tokens.T_PLUS_EQUAL; }
					break;
					
				case 45:
					// #line 245
					{ return Tokens.T_INC; }
					break;
					
				case 46:
					// #line 254
					{ return Tokens.T_MINUS_EQUAL; }
					break;
					
				case 47:
					// #line 270
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 48:
					// #line 246
					{ return Tokens.T_DEC; }
					break;
					
				case 49:
					// #line 255
					{ return Tokens.T_MUL_EQUAL; }
					break;
					
				case 50:
					// #line 257
					{ return Tokens.T_CONCAT_EQUAL; }
					break;
					
				case 51:
					// #line 261
					{ return Tokens.T_AND_EQUAL; }
					break;
					
				case 52:
					// #line 265
					{ return Tokens.T_BOOLEAN_AND; }
					break;
					
				case 53:
					// #line 262
					{ return Tokens.T_OR_EQUAL; }
					break;
					
				case 54:
					// #line 264
					{ return Tokens.T_BOOLEAN_OR; }
					break;
					
				case 55:
					// #line 263
					{ return Tokens.T_XOR_EQUAL; }
					break;
					
				case 56:
					// #line 271
					{ return Tokens.T_VARIABLE; }
					break;
					
				case 57:
					// #line 259
					{ return Tokens.T_SL_EQUAL; }
					break;
					
				case 58:
					// #line 204
					{ return Tokens.T_INT_TYPE; }
					break;
					
				case 59:
					// #line 329
					{ return Tokens.ErrorInvalidIdentifier; }
					break;
					
				case 60:
					// #line 184
					{ return Tokens.T_TRY; }
					break;
					
				case 61:
					// #line 172
					{ return Tokens.T_LOGICAL_AND; }
					break;
					
				case 62:
					// #line 159
					{ return Tokens.T_NEW; }
					break;
					
				case 63:
					// #line 199
					{ return Tokens.T_USE; }
					break;
					
				case 64:
					// #line 247
					{ return Tokens.T_IS_IDENTICAL; }
					break;
					
				case 65:
					// #line 260
					{ return Tokens.T_SR_EQUAL; }
					break;
					
				case 66:
					// #line 133
					{ return Tokens.T_EXIT; }
					break;
					
				case 67:
					// #line 173
					{ return Tokens.T_LOGICAL_XOR; }
					break;
					
				case 68:
					// #line 144
					{ return Tokens.T_FOR; }
					break;
					
				case 69:
					// #line 160
					{ return Tokens.T_VAR; }
					break;
					
				case 70:
					// #line 280
					{ return Tokens.ParseDouble; }
					break;
					
				case 71:
					// #line 248
					{ return Tokens.T_IS_NOT_IDENTICAL; }
					break;
					
				case 72:
					// #line 278
					{ return Tokens.ParseHexadecimalNumber; }
					break;
					
				case 73:
					// #line 238
					{ return Tokens.T_SELF; }
					break;
					
				case 74:
					// #line 151
					{ return Tokens.T_CASE; }
					break;
					
				case 75:
					// #line 328
					{ return Tokens.SingleQuotedIdentifier; }
					break;
					
				case 76:
					// #line 240
					{ return Tokens.T_TRUE; }
					break;
					
				case 77:
					// #line 174
					{ return Tokens.T_LIST; }
					break;
					
				case 78:
					// #line 242
					{ return Tokens.T_NULL; }
					break;
					
				case 79:
					// #line 201
					{ return Tokens.T_GOTO; }
					break;
					
				case 80:
					// #line 155
					{ return Tokens.T_ECHO; }
					break;
					
				case 81:
					// #line 140
					{ return Tokens.T_ELSE; }
					break;
					
				case 82:
					// #line 132
					{ return Tokens.T_EXIT; }
					break;
					
				case 83:
					// #line 161
					{ return Tokens.T_EVAL; }
					break;
					
				case 84:
					// #line 289
					{ BEGIN(LexicalStates.ST_DOC_COMMENT); yymore(); break; }
					break;
					
				case 85:
					// #line 212
					{ return Tokens.T_LINQ_FROM; }
					break;
					
				case 86:
					// #line 203
					{ return Tokens.T_BOOL_TYPE; }
					break;
					
				case 87:
					// #line 353
					{
						bool is_binary = GetTokenChar(0) != '<';
						hereDocLabel = GetTokenSubstring(is_binary ? 4 : 3).Trim();
						BEGIN(LexicalStates.ST_HEREDOC);
						return is_binary ? Tokens.T_BINARY_HEREDOC : Tokens.T_START_HEREDOC;
					}
					break;
					
				case 88:
					// #line 157
					{ return Tokens.T_CLASS; }
					break;
					
				case 89:
					// #line 189
					{ return Tokens.T_CLONE; }
					break;
					
				case 90:
					// #line 185
					{ return Tokens.T_CATCH; }
					break;
					
				case 91:
					// #line 135
					{ return Tokens.T_CONST; }
					break;
					
				case 92:
					// #line 167
					{ return Tokens.T_ISSET; }
					break;
					
				case 93:
					// #line 205
					{ return Tokens.T_INT64_TYPE; }
					break;
					
				case 94:
					// #line 156
					{ return Tokens.T_PRINT; }
					break;
					
				case 95:
					// #line 186
					{ return Tokens.T_THROW; }
					break;
					
				case 96:
					// #line 175
					{ return Tokens.T_ARRAY; }
					break;
					
				case 97:
					// #line 218
					{ return (InLinq) ? Tokens.T_LINQ_GROUP : Tokens.T_STRING; }
					break;
					
				case 98:
					// #line 170
					{ return Tokens.T_UNSET; }
					break;
					
				case 99:
					// #line 139
					{ return Tokens.T_ENDIF; }
					break;
					
				case 100:
					// #line 168
					{ return Tokens.T_EMPTY; }
					break;
					
				case 101:
					// #line 191
					{ return Tokens.T_FINAL; }
					break;
					
				case 102:
					// #line 241
					{ return Tokens.T_FALSE; }
					break;
					
				case 103:
					// #line 141
					{ return Tokens.T_WHILE; }
					break;
					
				case 104:
					// #line 213
					{ return (InLinq) ? Tokens.T_LINQ_WHERE : Tokens.T_STRING; }
					break;
					
				case 105:
					// #line 153
					{ return Tokens.T_BREAK; }
					break;
					
				case 106:
					// #line 229
					{ return Tokens.T_SET; }
					break;
					
				case 107:
					// #line 228
					{ return Tokens.T_GET; }
					break;
					
				case 108:
					// #line 294
					{ return Tokens.T_INT32_CAST; }
					break;
					
				case 109:
					// #line 207
					{ return Tokens.T_STRING_TYPE; }
					break;
					
				case 110:
					// #line 169
					{ return Tokens.T_STATIC; }
					break;
					
				case 111:
					// #line 217
					{ return (InLinq) ? Tokens.T_LINQ_SELECT : Tokens.T_STRING; }
					break;
					
				case 112:
					// #line 149
					{ return Tokens.T_SWITCH; }
					break;
					
				case 113:
					// #line 136
					{ return Tokens.T_RETURN; }
					break;
					
				case 114:
					// #line 200
					{ return Tokens.T_IMPORT; }
					break;
					
				case 115:
					// #line 237
					{ return Tokens.T_PARENT; }
					break;
					
				case 116:
					// #line 194
					{ return Tokens.T_PUBLIC; }
					break;
					
				case 117:
					// #line 227
					{ return Tokens.T_ASSERT; }
					break;
					
				case 118:
					// #line 166
					{ return Tokens.T_GLOBAL; }
					break;
					
				case 119:
					// #line 138
					{ return Tokens.T_ELSEIF; }
					break;
					
				case 120:
					// #line 145
					{ return Tokens.T_ENDFOR; }
					break;
					
				case 121:
					// #line 206
					{ return Tokens.T_DOUBLE_TYPE; }
					break;
					
				case 122:
					// #line 209
					{ return Tokens.T_OBJECT_TYPE; }
					break;
					
				case 123:
					// #line 230
					{ return Tokens.T_CALL; }
					break;
					
				case 124:
					// #line 300
					{ return Tokens.T_DOUBLE_CAST; }
					break;
					
				case 125:
					// #line 292
					{ return Tokens.T_INT8_CAST; }
					break;
					
				case 126:
					// #line 298
					{ return Tokens.T_UINT32_CAST; }
					break;
					
				case 127:
					// #line 307
					{ return Tokens.T_BOOL_CAST; }
					break;
					
				case 128:
					// #line 164
					{ return Tokens.T_REQUIRE; }
					break;
					
				case 129:
					// #line 162
					{ return Tokens.T_INCLUDE; }
					break;
					
				case 130:
					// #line 192
					{ return Tokens.T_PRIVATE; }
					break;
					
				case 131:
					// #line 222
					{ return Tokens.T_PARTIAL; }
					break;
					
				case 132:
					// #line 158
					{ return Tokens.T_EXTENDS; }
					break;
					
				case 133:
					// #line 128
					{
					  return Tokens.ErrorNotSupported; 
					}
					break;
					
				case 134:
					// #line 152
					{ return Tokens.T_DEFAULT; }
					break;
					
				case 135:
					// #line 146
					{ return Tokens.T_FOREACH; }
					break;
					
				case 136:
					// #line 214
					{ return (InLinq) ? Tokens.T_LINQ_ORDERBY : Tokens.T_STRING; }
					break;
					
				case 137:
					// #line 236
					{ return Tokens.T_SLEEP; }
					break;
					
				case 138:
					// #line 182
					{ return Tokens.T_DIR; }
					break;
					
				case 139:
					// #line 295
					{ return Tokens.T_INT64_CAST; }
					break;
					
				case 140:
					// #line 293
					{ return Tokens.T_INT16_CAST; }
					break;
					
				case 141:
					// #line 305
					{ return Tokens.T_ARRAY_CAST; }
					break;
					
				case 142:
					// #line 296
					{ return Tokens.T_UINT8_CAST; }
					break;
					
				case 143:
					// #line 308
					{ return Tokens.T_UNSET_CAST; }
					break;
					
				case 144:
					// #line 301
					{ return Tokens.T_FLOAT_CAST; }
					break;
					
				case 145:
					// #line 176
					{ return Tokens.T_CALLABLE; }
					break;
					
				case 146:
					// #line 154
					{ return Tokens.T_CONTINUE; }
					break;
					
				case 147:
					// #line 208
					{ return Tokens.T_RESOURCE_TYPE; }
					break;
					
				case 148:
					// #line 190
					{ return Tokens.T_ABSTRACT; }
					break;
					
				case 149:
					// #line 142
					{ return Tokens.T_ENDWHILE; }
					break;
					
				case 150:
					// #line 134
					{ return Tokens.T_FUNCTION; }
					break;
					
				case 151:
					// #line 180
					{ return Tokens.T_LINE; }
					break;
					
				case 152:
					// #line 181
					{ return Tokens.T_FILE; }
					break;
					
				case 153:
					// #line 235
					{ return Tokens.T_WAKEUP; }
					break;
					
				case 154:
					// #line 302
					{ return Tokens.T_STRING_CAST; }
					break;
					
				case 155:
					// #line 299
					{ return Tokens.T_UINT64_CAST; }
					break;
					
				case 156:
					// #line 297
					{ return Tokens.T_UINT16_CAST; }
					break;
					
				case 157:
					// #line 306
					{ return Tokens.T_OBJECT_CAST; }
					break;
					
				case 158:
					// #line 303
					{ return Tokens.T_BINARY_CAST; }
					break;
					
				case 159:
					// #line 210
					{ return Tokens.T_TYPEOF; }
					break;
					
				case 160:
					// #line 187
					{ return Tokens.T_INTERFACE; }
					break;
					
				case 161:
					// #line 193
					{ return Tokens.T_PROTECTED; }
					break;
					
				case 162:
					// #line 216
					{ return (InLinq) ? Tokens.T_LINQ_ASCENDING : Tokens.T_STRING; }
					break;
					
				case 163:
					// #line 198
					{ return Tokens.T_NAMESPACE; }
					break;
					
				case 164:
					// #line 150
					{ return Tokens.T_ENDSWITCH; }
					break;
					
				case 165:
					// #line 177
					{ return Tokens.T_CLASS_C; }
					break;
					
				case 166:
					// #line 304
					{ return Tokens.T_UNICODE_CAST; }
					break;
					
				case 167:
					// #line 195
					{ return Tokens.T_INSTANCEOF; }
					break;
					
				case 168:
					// #line 188
					{ return Tokens.T_IMPLEMENTS; }
					break;
					
				case 169:
					// #line 147
					{ return Tokens.T_ENDFOREACH; }
					break;
					
				case 170:
					// #line 215
					{ return (InLinq) ? Tokens.T_LINQ_DESCENDING : Tokens.T_STRING; }
					break;
					
				case 171:
					// #line 232
					{ return Tokens.T_TOSTRING; }
					break;
					
				case 172:
					// #line 239
					{ return Tokens.T_AUTOLOAD; }
					break;
					
				case 173:
					// #line 234
					{ return Tokens.T_DESTRUCT; }
					break;
					
				case 174:
					// #line 179
					{ return Tokens.T_METHOD_C; }
					break;
					
				case 175:
					// #line 233
					{ return Tokens.T_CONSTRUCT; }
					break;
					
				case 176:
					// #line 165
					{ return Tokens.T_REQUIRE_ONCE; }
					break;
					
				case 177:
					// #line 163
					{ return Tokens.T_INCLUDE_ONCE; }
					break;
					
				case 178:
					// #line 231
					{ return Tokens.T_CALLSTATIC; }
					break;
					
				case 179:
					// #line 178
					{ return Tokens.T_FUNC_C; }
					break;
					
				case 180:
					// #line 197
					{ return Tokens.T_NAMESPACE_C; }
					break;
					
				case 181:
					// #line 283
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_FILE; }
					break;
					
				case 182:
					// #line 282
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_LINE; }
					break;
					
				case 183:
					// #line 284
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_LINE; }
					break;
					
				case 184:
					// #line 285
					{ BEGIN(LexicalStates.ST_ONE_LINE_COMMENT); return Tokens.T_PRAGMA_DEFAULT_FILE; }
					break;
					
				case 185:
					// #line 486
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 186:
					// #line 478
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 187:
					// #line 469
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 188:
					// #line 479
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOUBLE_QUOTES; }
					break;
					
				case 189:
					// #line 468
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 190:
					// #line 485
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 191:
					// #line 487
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 192:
					// #line 483
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 193:
					// #line 482
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 194:
					// #line 480
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 195:
					// #line 481
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 196:
					// #line 477
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 197:
					// #line 473
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 198:
					// #line 475
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 199:
					// #line 472
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 200:
					// #line 474
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 201:
					// #line 470
					{ return Tokens.OctalCharCode; }
					break;
					
				case 202:
					// #line 476
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 203:
					// #line 484
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 204:
					// #line 471
					{ return Tokens.HexCharCode; }
					break;
					
				case 205:
					// #line 428
					{ yymore(); break; }
					break;
					
				case 206:
					// #line 429
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.SingleQuotedString; }
					break;
					
				case 207:
					// #line 509
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 208:
					// #line 502
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_BACKQUOTE; }
					break;
					
				case 209:
					// #line 492
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 210:
					// #line 501
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 211:
					// #line 491
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 212:
					// #line 507
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 213:
					// #line 510
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 214:
					// #line 506
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 215:
					// #line 505
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 216:
					// #line 503
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 217:
					// #line 504
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 218:
					// #line 500
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 219:
					// #line 497
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 220:
					// #line 496
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 221:
					// #line 498
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 222:
					// #line 495
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 223:
					// #line 493
					{ return Tokens.OctalCharCode; }
					break;
					
				case 224:
					// #line 499
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 225:
					// #line 508
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 226:
					// #line 494
					{ return Tokens.HexCharCode; }
					break;
					
				case 227:
					// #line 464
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 228:
					// #line 457
					{ return Tokens.T_ENCAPSED_AND_WHITESPACE; }
					break;
					
				case 229:
					// #line 449
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 230:
					// #line 448
					{ return Tokens.T_NUM_STRING; }
					break;
					
				case 231:
					// #line 462
					{ inString = true; return (Tokens)GetTokenChar(0); }
					break;
					
				case 232:
					// #line 465
					{ return Tokens.T_CHARACTER; }
					break;
					
				case 233:
					// #line 461
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_PROPERTY); inString = true; return Tokens.T_OBJECT_OPERATOR; }
					break;
					
				case 234:
					// #line 460
					{ yyless(1); return Tokens.T_CHARACTER; }
					break;
					
				case 235:
					// #line 458
					{ inString = true; return Tokens.T_VARIABLE; }
					break;
					
				case 236:
					// #line 459
					{ yy_push_state(LexicalStates.ST_LOOKING_FOR_VARNAME); return Tokens.T_DOLLAR_OPEN_CURLY_BRACES; }
					break;
					
				case 237:
					// #line 456
					{ return Tokens.T_BAD_CHARACTER; }
					break;
					
				case 238:
					// #line 453
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharName : Tokens.T_STRING; }
					break;
					
				case 239:
					// #line 454
					{ return Tokens.EscapedCharacter; }
					break;
					
				case 240:
					// #line 452
					{ inString = true; return (inUnicodeString) ? Tokens.UnicodeCharCode : Tokens.T_STRING; }
					break;
					
				case 241:
					// #line 450
					{ return Tokens.OctalCharCode; }
					break;
					
				case 242:
					// #line 455
					{ inString = true; return Tokens.T_STRING; }
					break;
					
				case 243:
					// #line 463
					{ yy_push_state(LexicalStates.ST_IN_SCRIPTING); yyless(1); return Tokens.T_CURLY_OPEN; }
					break;
					
				case 244:
					// #line 451
					{ return Tokens.HexCharCode; }
					break;
					
				case 245:
					// #line 433
					{
						if (IsCurrentHeredocEnd(0))
						{
						  yyless(hereDocLabel.Length);
						  hereDocLabel = null;
						  BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_END_HEREDOC;
						}
						else 
						{
							inString = true;
							return Tokens.T_STRING;
						}
					}
					break;
					
				case 246:
					// #line 373
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						break;
					}
					break;
					
				case 247:
					// #line 366
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						inString = (CurrentLexicalState != LexicalStates.ST_IN_SCRIPTING); 
						isCode = true;
						return Tokens.T_STRING;
					}
					break;
					
				case 248:
					// #line 387
					{
						yyless(0);
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						break;
					}
					break;
					
				case 249:
					// #line 381
					{
						if (!yy_pop_state()) return Tokens.ERROR;
						yy_push_state(LexicalStates.ST_IN_SCRIPTING);
						return Tokens.T_STRING_VARNAME;
					}
					break;
					
				case 250:
					// #line 422
					{ yymore(); break; }
					break;
					
				case 251:
					// #line 424
					{ yymore(); break; }
					break;
					
				case 252:
					// #line 423
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_DOC_COMMENT; }
					break;
					
				case 253:
					// #line 416
					{ yymore(); break; }
					break;
					
				case 254:
					// #line 418
					{ yymore(); break; }
					break;
					
				case 255:
					// #line 417
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_COMMENT; }
					break;
					
				case 256:
					// #line 396
					{ yymore(); break; }
					break;
					
				case 257:
					// #line 397
					{ yymore(); break; }
					break;
					
				case 258:
					// #line 398
					{ BEGIN(LexicalStates.ST_IN_SCRIPTING); return Tokens.T_LINE_COMMENT; }
					break;
					
				case 259:
					// #line 400
					{ 
					  if (AllowAspTags || GetTokenChar(TokenLength - 2) != '%') 
					  { 
							yyless(0);
							BEGIN(LexicalStates.ST_IN_SCRIPTING);
							return Tokens.T_LINE_COMMENT;
						} 
						else 
						{
							yymore();
							break;
						}
					}
					break;
					
				case 262: goto case 2;
				case 263: goto case 4;
				case 264: goto case 6;
				case 265: goto case 7;
				case 266: goto case 9;
				case 267: goto case 13;
				case 268: goto case 20;
				case 269: goto case 23;
				case 270: goto case 25;
				case 271: goto case 87;
				case 272: goto case 182;
				case 273: goto case 185;
				case 274: goto case 189;
				case 275: goto case 190;
				case 276: goto case 191;
				case 277: goto case 196;
				case 278: goto case 197;
				case 279: goto case 199;
				case 280: goto case 201;
				case 281: goto case 204;
				case 282: goto case 207;
				case 283: goto case 211;
				case 284: goto case 212;
				case 285: goto case 213;
				case 286: goto case 218;
				case 287: goto case 220;
				case 288: goto case 222;
				case 289: goto case 223;
				case 290: goto case 226;
				case 291: goto case 227;
				case 292: goto case 228;
				case 293: goto case 230;
				case 294: goto case 231;
				case 295: goto case 232;
				case 296: goto case 237;
				case 297: goto case 238;
				case 298: goto case 240;
				case 299: goto case 241;
				case 300: goto case 244;
				case 301: goto case 245;
				case 302: goto case 256;
				case 303: goto case 258;
				case 305: goto case 2;
				case 306: goto case 7;
				case 307: goto case 9;
				case 308: goto case 20;
				case 309: goto case 25;
				case 310: goto case 189;
				case 311: goto case 190;
				case 312: goto case 211;
				case 313: goto case 212;
				case 314: goto case 230;
				case 315: goto case 231;
				case 317: goto case 7;
				case 318: goto case 9;
				case 320: goto case 7;
				case 321: goto case 9;
				case 323: goto case 7;
				case 324: goto case 9;
				case 326: goto case 7;
				case 327: goto case 9;
				case 329: goto case 7;
				case 330: goto case 9;
				case 332: goto case 7;
				case 333: goto case 9;
				case 335: goto case 7;
				case 336: goto case 9;
				case 338: goto case 7;
				case 339: goto case 9;
				case 341: goto case 7;
				case 342: goto case 9;
				case 344: goto case 7;
				case 345: goto case 9;
				case 347: goto case 7;
				case 348: goto case 9;
				case 350: goto case 7;
				case 351: goto case 9;
				case 353: goto case 7;
				case 354: goto case 9;
				case 356: goto case 7;
				case 357: goto case 9;
				case 359: goto case 7;
				case 360: goto case 9;
				case 362: goto case 9;
				case 364: goto case 9;
				case 366: goto case 9;
				case 368: goto case 9;
				case 370: goto case 9;
				case 372: goto case 9;
				case 374: goto case 9;
				case 376: goto case 9;
				case 378: goto case 9;
				case 380: goto case 9;
				case 382: goto case 9;
				case 384: goto case 9;
				case 386: goto case 9;
				case 388: goto case 9;
				case 390: goto case 9;
				case 392: goto case 9;
				case 394: goto case 9;
				case 396: goto case 9;
				case 398: goto case 9;
				case 400: goto case 9;
				case 402: goto case 9;
				case 404: goto case 9;
				case 406: goto case 9;
				case 408: goto case 9;
				case 410: goto case 9;
				case 412: goto case 9;
				case 414: goto case 9;
				case 416: goto case 9;
				case 418: goto case 9;
				case 420: goto case 9;
				case 422: goto case 9;
				case 424: goto case 9;
				case 426: goto case 9;
				case 428: goto case 9;
				case 430: goto case 9;
				case 432: goto case 9;
				case 434: goto case 9;
				case 436: goto case 9;
				case 438: goto case 9;
				case 440: goto case 9;
				case 442: goto case 9;
				case 444: goto case 9;
				case 446: goto case 9;
				case 448: goto case 9;
				case 450: goto case 9;
				case 452: goto case 9;
				case 454: goto case 9;
				case 456: goto case 9;
				case 458: goto case 9;
				case 460: goto case 9;
				case 462: goto case 9;
				case 464: goto case 9;
				case 466: goto case 9;
				case 468: goto case 9;
				case 470: goto case 9;
				case 472: goto case 9;
				case 474: goto case 9;
				case 476: goto case 9;
				case 478: goto case 9;
				case 480: goto case 9;
				case 482: goto case 9;
				case 484: goto case 9;
				case 486: goto case 9;
				case 488: goto case 9;
				case 490: goto case 9;
				case 492: goto case 9;
				case 494: goto case 9;
				case 496: goto case 9;
				case 498: goto case 9;
				case 500: goto case 9;
				case 502: goto case 9;
				case 504: goto case 9;
				case 506: goto case 9;
				case 508: goto case 9;
				case 510: goto case 9;
				case 512: goto case 9;
				case 514: goto case 9;
				case 516: goto case 9;
				case 518: goto case 9;
				case 520: goto case 9;
				case 522: goto case 9;
				case 524: goto case 9;
				case 526: goto case 9;
				case 528: goto case 9;
				case 590: goto case 9;
				case 591: goto case 199;
				case 592: goto case 201;
				case 593: goto case 222;
				case 594: goto case 223;
				case 595: goto case 240;
				case 596: goto case 241;
				case 617: goto case 9;
				case 619: goto case 9;
				case 620: goto case 9;
				case 621: goto case 9;
				case 622: goto case 9;
				case 623: goto case 9;
				case 624: goto case 9;
				case 625: goto case 9;
				case 626: goto case 9;
				case 627: goto case 9;
				case 628: goto case 9;
				case 629: goto case 9;
				case 630: goto case 9;
				case 631: goto case 9;
				case 632: goto case 9;
				case 633: goto case 9;
				case 634: goto case 9;
				case 635: goto case 9;
				case 636: goto case 9;
				case 637: goto case 9;
				case 638: goto case 9;
				case 639: goto case 9;
				case 640: goto case 9;
				case 641: goto case 9;
				case 642: goto case 9;
				case 643: goto case 9;
				case 644: goto case 9;
				case 645: goto case 9;
				case 646: goto case 9;
				case 647: goto case 9;
				case 648: goto case 9;
				case 649: goto case 9;
				case 650: goto case 9;
				case 651: goto case 9;
				case 652: goto case 9;
				case 653: goto case 9;
				case 654: goto case 9;
				case 655: goto case 9;
				case 656: goto case 9;
				case 657: goto case 9;
				case 658: goto case 9;
				case 659: goto case 9;
				case 660: goto case 9;
				case 661: goto case 9;
				case 662: goto case 9;
				case 663: goto case 9;
				case 664: goto case 9;
				case 665: goto case 9;
				case 666: goto case 9;
				case 667: goto case 9;
				case 668: goto case 9;
				case 669: goto case 9;
				case 670: goto case 9;
				case 671: goto case 9;
				case 672: goto case 9;
				case 673: goto case 9;
				case 674: goto case 9;
				case 675: goto case 9;
				case 676: goto case 9;
				case 677: goto case 9;
				case 678: goto case 9;
				case 679: goto case 9;
				case 680: goto case 9;
				case 681: goto case 9;
				case 682: goto case 9;
				case 683: goto case 9;
				case 684: goto case 9;
				case 685: goto case 9;
				case 686: goto case 9;
				case 687: goto case 9;
				case 688: goto case 9;
				case 689: goto case 9;
				case 690: goto case 9;
				case 691: goto case 9;
				case 692: goto case 9;
				case 693: goto case 9;
				case 694: goto case 9;
				case 695: goto case 9;
				case 696: goto case 9;
				case 697: goto case 9;
				case 698: goto case 9;
				case 699: goto case 9;
				case 700: goto case 9;
				case 701: goto case 9;
				case 702: goto case 9;
				case 703: goto case 9;
				case 704: goto case 9;
				case 705: goto case 9;
				case 706: goto case 9;
				case 707: goto case 9;
				case 708: goto case 9;
				case 709: goto case 9;
				case 710: goto case 9;
				case 711: goto case 9;
				case 712: goto case 9;
				case 713: goto case 9;
				case 714: goto case 9;
				case 715: goto case 9;
				case 716: goto case 9;
				case 717: goto case 9;
				case 718: goto case 9;
				case 719: goto case 9;
				case 720: goto case 9;
				case 721: goto case 9;
				case 722: goto case 9;
				case 723: goto case 9;
				case 724: goto case 9;
				case 725: goto case 9;
				case 726: goto case 9;
				case 727: goto case 9;
				case 728: goto case 9;
				case 729: goto case 9;
				case 730: goto case 9;
				case 731: goto case 9;
				case 732: goto case 9;
				case 733: goto case 9;
				case 734: goto case 9;
				case 735: goto case 9;
				case 736: goto case 9;
				case 737: goto case 9;
				case 738: goto case 9;
				case 739: goto case 9;
				case 740: goto case 9;
				case 741: goto case 9;
				case 742: goto case 9;
				case 743: goto case 9;
				case 744: goto case 9;
				case 745: goto case 9;
				case 746: goto case 9;
				case 747: goto case 9;
				case 748: goto case 9;
				case 749: goto case 9;
				case 750: goto case 9;
				case 751: goto case 9;
				case 752: goto case 9;
				case 753: goto case 9;
				case 754: goto case 9;
				case 755: goto case 9;
				case 756: goto case 9;
				case 757: goto case 9;
				case 758: goto case 9;
				case 759: goto case 9;
				case 760: goto case 9;
				case 761: goto case 9;
				case 762: goto case 9;
				case 763: goto case 9;
				case 764: goto case 9;
				case 765: goto case 9;
				case 766: goto case 9;
				case 767: goto case 9;
				case 768: goto case 9;
				case 769: goto case 9;
				case 770: goto case 9;
				case 771: goto case 9;
				case 772: goto case 9;
				case 773: goto case 9;
				case 774: goto case 9;
				case 775: goto case 9;
				case 776: goto case 9;
				case 777: goto case 9;
				case 778: goto case 9;
				case 779: goto case 9;
				case 780: goto case 9;
				case 781: goto case 9;
				case 782: goto case 9;
				case 783: goto case 9;
				case 784: goto case 9;
				case 785: goto case 199;
				case 786: goto case 222;
				case 787: goto case 240;
				case 790: goto case 9;
				case 791: goto case 9;
				case 792: goto case 9;
				case 793: goto case 9;
				case 794: goto case 9;
				case 795: goto case 9;
				case 796: goto case 9;
				case 797: goto case 9;
				case 798: goto case 9;
				case 799: goto case 9;
				case 800: goto case 9;
				case 801: goto case 9;
				case 802: goto case 9;
				case 803: goto case 9;
				case 804: goto case 9;
				case 805: goto case 9;
				case 806: goto case 9;
				case 807: goto case 9;
				case 808: goto case 9;
				case 809: goto case 9;
				case 810: goto case 9;
				case 811: goto case 9;
				case 812: goto case 9;
				case 813: goto case 9;
				case 814: goto case 9;
				case 815: goto case 9;
				case 816: goto case 9;
				case 817: goto case 9;
				case 818: goto case 9;
				case 819: goto case 9;
				case 820: goto case 9;
				case 821: goto case 9;
				case 822: goto case 9;
				case 823: goto case 9;
				case 824: goto case 9;
				case 825: goto case 9;
				case 826: goto case 9;
				case 827: goto case 9;
				case 828: goto case 9;
				case 829: goto case 9;
				case 830: goto case 9;
				case 831: goto case 9;
				case 832: goto case 9;
				case 833: goto case 9;
				case 834: goto case 9;
				case 835: goto case 9;
				case 836: goto case 9;
				case 837: goto case 9;
				case 838: goto case 9;
				case 839: goto case 9;
				case 840: goto case 9;
				case 841: goto case 9;
				case 842: goto case 9;
				case 843: goto case 9;
				case 844: goto case 9;
				case 845: goto case 9;
				case 846: goto case 9;
				case 847: goto case 9;
				case 848: goto case 9;
				case 849: goto case 9;
				case 850: goto case 9;
				case 851: goto case 9;
				case 852: goto case 9;
				case 853: goto case 9;
				case 854: goto case 9;
				case 855: goto case 9;
				case 856: goto case 9;
				case 857: goto case 9;
				case 858: goto case 9;
				case 859: goto case 9;
				case 860: goto case 9;
				case 861: goto case 9;
				case 862: goto case 9;
				case 863: goto case 9;
				case 864: goto case 9;
				case 865: goto case 9;
				case 866: goto case 9;
				case 867: goto case 9;
				case 868: goto case 9;
				case 869: goto case 9;
				case 870: goto case 9;
				case 871: goto case 9;
				case 872: goto case 9;
				case 873: goto case 9;
				case 874: goto case 9;
				case 875: goto case 9;
				case 876: goto case 9;
				case 877: goto case 9;
				case 878: goto case 9;
				case 879: goto case 9;
				case 880: goto case 9;
				case 881: goto case 9;
				case 882: goto case 9;
				case 883: goto case 9;
				case 884: goto case 9;
				case 885: goto case 9;
				case 886: goto case 199;
				case 887: goto case 222;
				case 888: goto case 240;
				case 890: goto case 9;
				case 891: goto case 9;
				case 892: goto case 9;
				case 893: goto case 9;
				case 894: goto case 9;
				case 895: goto case 9;
				case 896: goto case 9;
				case 897: goto case 199;
				case 898: goto case 222;
				case 899: goto case 240;
				case 900: goto case 199;
				case 901: goto case 222;
				case 902: goto case 240;
			}
			accepted = false;
			return yyreturn;
		}
		
		#pragma warning restore 162
		
		
		#endregion
		private void BEGIN(LexicalStates state)
		{
			current_lexical_state = state;
		}
		
		private char Advance()
		{
			if (lookahead_index >= chars_read)
			{
				if (token_start > 0)
				{
					// shift buffer left:
					int length = chars_read - token_start;
					System.Buffer.BlockCopy(buffer, token_start << 1, buffer, 0, length << 1);
					token_end -= token_start;
					token_chunk_start -= token_start;
					token_start = 0;
					chars_read = lookahead_index = length;
					
					// populate the remaining bytes:
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					
					chars_read += count;
				}
				
				while (lookahead_index >= chars_read)
				{
					if (lookahead_index >= buffer.Length)
						buffer = ResizeBuffer(buffer);
					
					int count = reader.Read(buffer, chars_read, buffer.Length - chars_read);
					if (count <= 0) return EOF;
					chars_read += count;
				}
			}
			
			return Map(buffer[lookahead_index++]);
		}
		
		private char[] ResizeBuffer(char[] buf)
		{
			char[] result = new char[buf.Length << 1];
			System.Buffer.BlockCopy(buf, 0, result, 0, buf.Length << 1);
			return result;
		}
		
		private void AdvanceEndPosition(int from, int to)
		{
			int last_eoln = from - token_end_pos.Column;
		
		    for (int i = from; i < to; i++)
			{
		        char ch = buffer[i];
		        
		        // Line endings supported by Visual Studio:
		
		        // CRLF:  Windows, U+000D + U+000A
		        // LF:    Unix, U+000A
		        // CR:    Mac, U+000D
		        // LS:    Line Separator, U+2028
		        // PS:    Paragraph Separator, U+2029
		
		        if ((ch == '\n') || // CRLF, LF
		            (ch == '\r' && ((i + 1 < buffer.Length) ? buffer[i + 1] : '\0') != '\n') ||    // CR, not CRLF
		            (ch == (char)0x2028) || 
		            (ch == (char)0x2029))
		        {
		            token_end_pos.Line++;
		            last_eoln = i;
		        }
			}
		
			token_end_pos.Char += to - from;
			token_end_pos.Column = to - last_eoln;
		}
		
		protected static bool IsNewLineCharacter(char ch)
		{
		    return ch == '\r' || ch == '\n' || ch == (char)0x2028 || ch == (char)0x2029;
		}
		private void TrimTokenEnd()
		{
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\n')
				token_end--;
			if (token_end > token_chunk_start && buffer[token_end - 1] == '\r')
				token_end--;
			}
		
		private void MarkTokenChunkStart()
		{
			token_chunk_start = lookahead_index;
		}
		
		private void MarkTokenEnd()
		{
			token_end = lookahead_index;
		}
		
		private void MoveToTokenEnd()
		{
			lookahead_index = token_end;
			yy_at_bol = (token_end > token_chunk_start) && (buffer[token_end - 1] == '\r' || buffer[token_end - 1] == '\n');
		}
		
		public int TokenLength
		{
			get { return token_end - token_start; }
		}
		
		public int TokenChunkLength
		{
			get { return token_end - token_chunk_start; }
		}
		
		private void yymore()
		{
			if (!expanding_token)
			{
				token_start = token_chunk_start;
				expanding_token = true;
			}
		}
		
		private void yyless(int count)
		{
			lookahead_index = token_end = token_chunk_start + count;
		}
		
		private Stack<LexicalStates> stateStack = new Stack<LexicalStates>(20);
		
		private void yy_push_state(LexicalStates state)
		{
			stateStack.Push(current_lexical_state);
			current_lexical_state = state;
		}
		
		private bool yy_pop_state()
		{
			if (stateStack.Count == 0) return false;
			current_lexical_state = stateStack.Pop();
			return true;
		}
		
		private LexicalStates yy_top_state()
		{
			return stateStack.Peek();
		}
		
		#region Tables
		
		private static AcceptConditions[] acceptCondition = new AcceptConditions[]
		{
			AcceptConditions.NotAccept, // 0
			AcceptConditions.Accept, // 1
			AcceptConditions.Accept, // 2
			AcceptConditions.Accept, // 3
			AcceptConditions.Accept, // 4
			AcceptConditions.Accept, // 5
			AcceptConditions.Accept, // 6
			AcceptConditions.Accept, // 7
			AcceptConditions.Accept, // 8
			AcceptConditions.Accept, // 9
			AcceptConditions.Accept, // 10
			AcceptConditions.Accept, // 11
			AcceptConditions.Accept, // 12
			AcceptConditions.Accept, // 13
			AcceptConditions.Accept, // 14
			AcceptConditions.Accept, // 15
			AcceptConditions.Accept, // 16
			AcceptConditions.Accept, // 17
			AcceptConditions.Accept, // 18
			AcceptConditions.Accept, // 19
			AcceptConditions.Accept, // 20
			AcceptConditions.Accept, // 21
			AcceptConditions.Accept, // 22
			AcceptConditions.Accept, // 23
			AcceptConditions.Accept, // 24
			AcceptConditions.Accept, // 25
			AcceptConditions.Accept, // 26
			AcceptConditions.Accept, // 27
			AcceptConditions.Accept, // 28
			AcceptConditions.Accept, // 29
			AcceptConditions.Accept, // 30
			AcceptConditions.Accept, // 31
			AcceptConditions.Accept, // 32
			AcceptConditions.Accept, // 33
			AcceptConditions.Accept, // 34
			AcceptConditions.Accept, // 35
			AcceptConditions.Accept, // 36
			AcceptConditions.Accept, // 37
			AcceptConditions.Accept, // 38
			AcceptConditions.Accept, // 39
			AcceptConditions.Accept, // 40
			AcceptConditions.Accept, // 41
			AcceptConditions.Accept, // 42
			AcceptConditions.Accept, // 43
			AcceptConditions.Accept, // 44
			AcceptConditions.Accept, // 45
			AcceptConditions.Accept, // 46
			AcceptConditions.Accept, // 47
			AcceptConditions.Accept, // 48
			AcceptConditions.Accept, // 49
			AcceptConditions.Accept, // 50
			AcceptConditions.Accept, // 51
			AcceptConditions.Accept, // 52
			AcceptConditions.Accept, // 53
			AcceptConditions.Accept, // 54
			AcceptConditions.Accept, // 55
			AcceptConditions.Accept, // 56
			AcceptConditions.Accept, // 57
			AcceptConditions.Accept, // 58
			AcceptConditions.Accept, // 59
			AcceptConditions.Accept, // 60
			AcceptConditions.Accept, // 61
			AcceptConditions.Accept, // 62
			AcceptConditions.Accept, // 63
			AcceptConditions.Accept, // 64
			AcceptConditions.Accept, // 65
			AcceptConditions.Accept, // 66
			AcceptConditions.Accept, // 67
			AcceptConditions.Accept, // 68
			AcceptConditions.Accept, // 69
			AcceptConditions.Accept, // 70
			AcceptConditions.Accept, // 71
			AcceptConditions.Accept, // 72
			AcceptConditions.Accept, // 73
			AcceptConditions.Accept, // 74
			AcceptConditions.Accept, // 75
			AcceptConditions.Accept, // 76
			AcceptConditions.Accept, // 77
			AcceptConditions.Accept, // 78
			AcceptConditions.Accept, // 79
			AcceptConditions.Accept, // 80
			AcceptConditions.Accept, // 81
			AcceptConditions.Accept, // 82
			AcceptConditions.Accept, // 83
			AcceptConditions.Accept, // 84
			AcceptConditions.Accept, // 85
			AcceptConditions.Accept, // 86
			AcceptConditions.Accept, // 87
			AcceptConditions.Accept, // 88
			AcceptConditions.Accept, // 89
			AcceptConditions.Accept, // 90
			AcceptConditions.Accept, // 91
			AcceptConditions.Accept, // 92
			AcceptConditions.Accept, // 93
			AcceptConditions.Accept, // 94
			AcceptConditions.Accept, // 95
			AcceptConditions.Accept, // 96
			AcceptConditions.Accept, // 97
			AcceptConditions.Accept, // 98
			AcceptConditions.Accept, // 99
			AcceptConditions.Accept, // 100
			AcceptConditions.Accept, // 101
			AcceptConditions.Accept, // 102
			AcceptConditions.Accept, // 103
			AcceptConditions.Accept, // 104
			AcceptConditions.Accept, // 105
			AcceptConditions.Accept, // 106
			AcceptConditions.Accept, // 107
			AcceptConditions.Accept, // 108
			AcceptConditions.Accept, // 109
			AcceptConditions.Accept, // 110
			AcceptConditions.Accept, // 111
			AcceptConditions.Accept, // 112
			AcceptConditions.Accept, // 113
			AcceptConditions.Accept, // 114
			AcceptConditions.Accept, // 115
			AcceptConditions.Accept, // 116
			AcceptConditions.Accept, // 117
			AcceptConditions.Accept, // 118
			AcceptConditions.Accept, // 119
			AcceptConditions.Accept, // 120
			AcceptConditions.Accept, // 121
			AcceptConditions.Accept, // 122
			AcceptConditions.Accept, // 123
			AcceptConditions.Accept, // 124
			AcceptConditions.Accept, // 125
			AcceptConditions.Accept, // 126
			AcceptConditions.Accept, // 127
			AcceptConditions.Accept, // 128
			AcceptConditions.Accept, // 129
			AcceptConditions.Accept, // 130
			AcceptConditions.Accept, // 131
			AcceptConditions.Accept, // 132
			AcceptConditions.Accept, // 133
			AcceptConditions.Accept, // 134
			AcceptConditions.Accept, // 135
			AcceptConditions.Accept, // 136
			AcceptConditions.Accept, // 137
			AcceptConditions.Accept, // 138
			AcceptConditions.Accept, // 139
			AcceptConditions.Accept, // 140
			AcceptConditions.Accept, // 141
			AcceptConditions.Accept, // 142
			AcceptConditions.Accept, // 143
			AcceptConditions.Accept, // 144
			AcceptConditions.Accept, // 145
			AcceptConditions.Accept, // 146
			AcceptConditions.Accept, // 147
			AcceptConditions.Accept, // 148
			AcceptConditions.Accept, // 149
			AcceptConditions.Accept, // 150
			AcceptConditions.Accept, // 151
			AcceptConditions.Accept, // 152
			AcceptConditions.Accept, // 153
			AcceptConditions.Accept, // 154
			AcceptConditions.Accept, // 155
			AcceptConditions.Accept, // 156
			AcceptConditions.Accept, // 157
			AcceptConditions.Accept, // 158
			AcceptConditions.Accept, // 159
			AcceptConditions.Accept, // 160
			AcceptConditions.Accept, // 161
			AcceptConditions.Accept, // 162
			AcceptConditions.Accept, // 163
			AcceptConditions.Accept, // 164
			AcceptConditions.Accept, // 165
			AcceptConditions.Accept, // 166
			AcceptConditions.Accept, // 167
			AcceptConditions.Accept, // 168
			AcceptConditions.Accept, // 169
			AcceptConditions.Accept, // 170
			AcceptConditions.Accept, // 171
			AcceptConditions.Accept, // 172
			AcceptConditions.Accept, // 173
			AcceptConditions.Accept, // 174
			AcceptConditions.Accept, // 175
			AcceptConditions.Accept, // 176
			AcceptConditions.Accept, // 177
			AcceptConditions.Accept, // 178
			AcceptConditions.Accept, // 179
			AcceptConditions.Accept, // 180
			AcceptConditions.Accept, // 181
			AcceptConditions.Accept, // 182
			AcceptConditions.Accept, // 183
			AcceptConditions.Accept, // 184
			AcceptConditions.Accept, // 185
			AcceptConditions.Accept, // 186
			AcceptConditions.Accept, // 187
			AcceptConditions.Accept, // 188
			AcceptConditions.Accept, // 189
			AcceptConditions.Accept, // 190
			AcceptConditions.Accept, // 191
			AcceptConditions.Accept, // 192
			AcceptConditions.Accept, // 193
			AcceptConditions.Accept, // 194
			AcceptConditions.Accept, // 195
			AcceptConditions.Accept, // 196
			AcceptConditions.Accept, // 197
			AcceptConditions.Accept, // 198
			AcceptConditions.Accept, // 199
			AcceptConditions.Accept, // 200
			AcceptConditions.Accept, // 201
			AcceptConditions.Accept, // 202
			AcceptConditions.Accept, // 203
			AcceptConditions.Accept, // 204
			AcceptConditions.Accept, // 205
			AcceptConditions.Accept, // 206
			AcceptConditions.Accept, // 207
			AcceptConditions.Accept, // 208
			AcceptConditions.Accept, // 209
			AcceptConditions.Accept, // 210
			AcceptConditions.Accept, // 211
			AcceptConditions.Accept, // 212
			AcceptConditions.Accept, // 213
			AcceptConditions.Accept, // 214
			AcceptConditions.Accept, // 215
			AcceptConditions.Accept, // 216
			AcceptConditions.Accept, // 217
			AcceptConditions.Accept, // 218
			AcceptConditions.Accept, // 219
			AcceptConditions.Accept, // 220
			AcceptConditions.Accept, // 221
			AcceptConditions.Accept, // 222
			AcceptConditions.Accept, // 223
			AcceptConditions.Accept, // 224
			AcceptConditions.Accept, // 225
			AcceptConditions.Accept, // 226
			AcceptConditions.Accept, // 227
			AcceptConditions.Accept, // 228
			AcceptConditions.Accept, // 229
			AcceptConditions.Accept, // 230
			AcceptConditions.Accept, // 231
			AcceptConditions.Accept, // 232
			AcceptConditions.Accept, // 233
			AcceptConditions.Accept, // 234
			AcceptConditions.Accept, // 235
			AcceptConditions.Accept, // 236
			AcceptConditions.Accept, // 237
			AcceptConditions.Accept, // 238
			AcceptConditions.Accept, // 239
			AcceptConditions.Accept, // 240
			AcceptConditions.Accept, // 241
			AcceptConditions.Accept, // 242
			AcceptConditions.Accept, // 243
			AcceptConditions.Accept, // 244
			AcceptConditions.AcceptOnStart, // 245
			AcceptConditions.Accept, // 246
			AcceptConditions.Accept, // 247
			AcceptConditions.Accept, // 248
			AcceptConditions.Accept, // 249
			AcceptConditions.Accept, // 250
			AcceptConditions.Accept, // 251
			AcceptConditions.Accept, // 252
			AcceptConditions.Accept, // 253
			AcceptConditions.Accept, // 254
			AcceptConditions.Accept, // 255
			AcceptConditions.Accept, // 256
			AcceptConditions.Accept, // 257
			AcceptConditions.Accept, // 258
			AcceptConditions.Accept, // 259
			AcceptConditions.NotAccept, // 260
			AcceptConditions.Accept, // 261
			AcceptConditions.Accept, // 262
			AcceptConditions.Accept, // 263
			AcceptConditions.Accept, // 264
			AcceptConditions.Accept, // 265
			AcceptConditions.Accept, // 266
			AcceptConditions.Accept, // 267
			AcceptConditions.Accept, // 268
			AcceptConditions.Accept, // 269
			AcceptConditions.Accept, // 270
			AcceptConditions.Accept, // 271
			AcceptConditions.Accept, // 272
			AcceptConditions.Accept, // 273
			AcceptConditions.Accept, // 274
			AcceptConditions.Accept, // 275
			AcceptConditions.Accept, // 276
			AcceptConditions.Accept, // 277
			AcceptConditions.Accept, // 278
			AcceptConditions.Accept, // 279
			AcceptConditions.Accept, // 280
			AcceptConditions.Accept, // 281
			AcceptConditions.Accept, // 282
			AcceptConditions.Accept, // 283
			AcceptConditions.Accept, // 284
			AcceptConditions.Accept, // 285
			AcceptConditions.Accept, // 286
			AcceptConditions.Accept, // 287
			AcceptConditions.Accept, // 288
			AcceptConditions.Accept, // 289
			AcceptConditions.Accept, // 290
			AcceptConditions.Accept, // 291
			AcceptConditions.Accept, // 292
			AcceptConditions.Accept, // 293
			AcceptConditions.Accept, // 294
			AcceptConditions.Accept, // 295
			AcceptConditions.Accept, // 296
			AcceptConditions.Accept, // 297
			AcceptConditions.Accept, // 298
			AcceptConditions.Accept, // 299
			AcceptConditions.Accept, // 300
			AcceptConditions.AcceptOnStart, // 301
			AcceptConditions.Accept, // 302
			AcceptConditions.Accept, // 303
			AcceptConditions.NotAccept, // 304
			AcceptConditions.Accept, // 305
			AcceptConditions.Accept, // 306
			AcceptConditions.Accept, // 307
			AcceptConditions.Accept, // 308
			AcceptConditions.Accept, // 309
			AcceptConditions.Accept, // 310
			AcceptConditions.Accept, // 311
			AcceptConditions.Accept, // 312
			AcceptConditions.Accept, // 313
			AcceptConditions.Accept, // 314
			AcceptConditions.Accept, // 315
			AcceptConditions.NotAccept, // 316
			AcceptConditions.Accept, // 317
			AcceptConditions.Accept, // 318
			AcceptConditions.NotAccept, // 319
			AcceptConditions.Accept, // 320
			AcceptConditions.Accept, // 321
			AcceptConditions.NotAccept, // 322
			AcceptConditions.Accept, // 323
			AcceptConditions.Accept, // 324
			AcceptConditions.NotAccept, // 325
			AcceptConditions.Accept, // 326
			AcceptConditions.Accept, // 327
			AcceptConditions.NotAccept, // 328
			AcceptConditions.Accept, // 329
			AcceptConditions.Accept, // 330
			AcceptConditions.NotAccept, // 331
			AcceptConditions.Accept, // 332
			AcceptConditions.Accept, // 333
			AcceptConditions.NotAccept, // 334
			AcceptConditions.Accept, // 335
			AcceptConditions.Accept, // 336
			AcceptConditions.NotAccept, // 337
			AcceptConditions.Accept, // 338
			AcceptConditions.Accept, // 339
			AcceptConditions.NotAccept, // 340
			AcceptConditions.Accept, // 341
			AcceptConditions.Accept, // 342
			AcceptConditions.NotAccept, // 343
			AcceptConditions.Accept, // 344
			AcceptConditions.Accept, // 345
			AcceptConditions.NotAccept, // 346
			AcceptConditions.Accept, // 347
			AcceptConditions.Accept, // 348
			AcceptConditions.NotAccept, // 349
			AcceptConditions.Accept, // 350
			AcceptConditions.Accept, // 351
			AcceptConditions.NotAccept, // 352
			AcceptConditions.Accept, // 353
			AcceptConditions.Accept, // 354
			AcceptConditions.NotAccept, // 355
			AcceptConditions.Accept, // 356
			AcceptConditions.Accept, // 357
			AcceptConditions.NotAccept, // 358
			AcceptConditions.Accept, // 359
			AcceptConditions.Accept, // 360
			AcceptConditions.NotAccept, // 361
			AcceptConditions.Accept, // 362
			AcceptConditions.NotAccept, // 363
			AcceptConditions.Accept, // 364
			AcceptConditions.NotAccept, // 365
			AcceptConditions.Accept, // 366
			AcceptConditions.NotAccept, // 367
			AcceptConditions.Accept, // 368
			AcceptConditions.NotAccept, // 369
			AcceptConditions.Accept, // 370
			AcceptConditions.NotAccept, // 371
			AcceptConditions.Accept, // 372
			AcceptConditions.NotAccept, // 373
			AcceptConditions.Accept, // 374
			AcceptConditions.NotAccept, // 375
			AcceptConditions.Accept, // 376
			AcceptConditions.NotAccept, // 377
			AcceptConditions.Accept, // 378
			AcceptConditions.NotAccept, // 379
			AcceptConditions.Accept, // 380
			AcceptConditions.NotAccept, // 381
			AcceptConditions.Accept, // 382
			AcceptConditions.NotAccept, // 383
			AcceptConditions.Accept, // 384
			AcceptConditions.NotAccept, // 385
			AcceptConditions.Accept, // 386
			AcceptConditions.NotAccept, // 387
			AcceptConditions.Accept, // 388
			AcceptConditions.NotAccept, // 389
			AcceptConditions.Accept, // 390
			AcceptConditions.NotAccept, // 391
			AcceptConditions.Accept, // 392
			AcceptConditions.NotAccept, // 393
			AcceptConditions.Accept, // 394
			AcceptConditions.NotAccept, // 395
			AcceptConditions.Accept, // 396
			AcceptConditions.NotAccept, // 397
			AcceptConditions.Accept, // 398
			AcceptConditions.NotAccept, // 399
			AcceptConditions.Accept, // 400
			AcceptConditions.NotAccept, // 401
			AcceptConditions.Accept, // 402
			AcceptConditions.NotAccept, // 403
			AcceptConditions.Accept, // 404
			AcceptConditions.NotAccept, // 405
			AcceptConditions.Accept, // 406
			AcceptConditions.NotAccept, // 407
			AcceptConditions.Accept, // 408
			AcceptConditions.NotAccept, // 409
			AcceptConditions.Accept, // 410
			AcceptConditions.NotAccept, // 411
			AcceptConditions.Accept, // 412
			AcceptConditions.NotAccept, // 413
			AcceptConditions.Accept, // 414
			AcceptConditions.NotAccept, // 415
			AcceptConditions.Accept, // 416
			AcceptConditions.NotAccept, // 417
			AcceptConditions.Accept, // 418
			AcceptConditions.NotAccept, // 419
			AcceptConditions.Accept, // 420
			AcceptConditions.NotAccept, // 421
			AcceptConditions.Accept, // 422
			AcceptConditions.NotAccept, // 423
			AcceptConditions.Accept, // 424
			AcceptConditions.NotAccept, // 425
			AcceptConditions.Accept, // 426
			AcceptConditions.NotAccept, // 427
			AcceptConditions.Accept, // 428
			AcceptConditions.NotAccept, // 429
			AcceptConditions.Accept, // 430
			AcceptConditions.NotAccept, // 431
			AcceptConditions.Accept, // 432
			AcceptConditions.NotAccept, // 433
			AcceptConditions.Accept, // 434
			AcceptConditions.NotAccept, // 435
			AcceptConditions.Accept, // 436
			AcceptConditions.NotAccept, // 437
			AcceptConditions.Accept, // 438
			AcceptConditions.NotAccept, // 439
			AcceptConditions.Accept, // 440
			AcceptConditions.NotAccept, // 441
			AcceptConditions.Accept, // 442
			AcceptConditions.NotAccept, // 443
			AcceptConditions.Accept, // 444
			AcceptConditions.NotAccept, // 445
			AcceptConditions.Accept, // 446
			AcceptConditions.NotAccept, // 447
			AcceptConditions.Accept, // 448
			AcceptConditions.NotAccept, // 449
			AcceptConditions.Accept, // 450
			AcceptConditions.NotAccept, // 451
			AcceptConditions.Accept, // 452
			AcceptConditions.NotAccept, // 453
			AcceptConditions.Accept, // 454
			AcceptConditions.NotAccept, // 455
			AcceptConditions.Accept, // 456
			AcceptConditions.NotAccept, // 457
			AcceptConditions.Accept, // 458
			AcceptConditions.NotAccept, // 459
			AcceptConditions.Accept, // 460
			AcceptConditions.NotAccept, // 461
			AcceptConditions.Accept, // 462
			AcceptConditions.NotAccept, // 463
			AcceptConditions.Accept, // 464
			AcceptConditions.NotAccept, // 465
			AcceptConditions.Accept, // 466
			AcceptConditions.NotAccept, // 467
			AcceptConditions.Accept, // 468
			AcceptConditions.NotAccept, // 469
			AcceptConditions.Accept, // 470
			AcceptConditions.NotAccept, // 471
			AcceptConditions.Accept, // 472
			AcceptConditions.NotAccept, // 473
			AcceptConditions.Accept, // 474
			AcceptConditions.NotAccept, // 475
			AcceptConditions.Accept, // 476
			AcceptConditions.NotAccept, // 477
			AcceptConditions.Accept, // 478
			AcceptConditions.NotAccept, // 479
			AcceptConditions.Accept, // 480
			AcceptConditions.NotAccept, // 481
			AcceptConditions.Accept, // 482
			AcceptConditions.NotAccept, // 483
			AcceptConditions.Accept, // 484
			AcceptConditions.NotAccept, // 485
			AcceptConditions.Accept, // 486
			AcceptConditions.NotAccept, // 487
			AcceptConditions.Accept, // 488
			AcceptConditions.NotAccept, // 489
			AcceptConditions.Accept, // 490
			AcceptConditions.NotAccept, // 491
			AcceptConditions.Accept, // 492
			AcceptConditions.NotAccept, // 493
			AcceptConditions.Accept, // 494
			AcceptConditions.NotAccept, // 495
			AcceptConditions.Accept, // 496
			AcceptConditions.NotAccept, // 497
			AcceptConditions.Accept, // 498
			AcceptConditions.NotAccept, // 499
			AcceptConditions.Accept, // 500
			AcceptConditions.NotAccept, // 501
			AcceptConditions.Accept, // 502
			AcceptConditions.NotAccept, // 503
			AcceptConditions.Accept, // 504
			AcceptConditions.NotAccept, // 505
			AcceptConditions.Accept, // 506
			AcceptConditions.NotAccept, // 507
			AcceptConditions.Accept, // 508
			AcceptConditions.NotAccept, // 509
			AcceptConditions.Accept, // 510
			AcceptConditions.NotAccept, // 511
			AcceptConditions.Accept, // 512
			AcceptConditions.NotAccept, // 513
			AcceptConditions.Accept, // 514
			AcceptConditions.NotAccept, // 515
			AcceptConditions.Accept, // 516
			AcceptConditions.NotAccept, // 517
			AcceptConditions.Accept, // 518
			AcceptConditions.NotAccept, // 519
			AcceptConditions.Accept, // 520
			AcceptConditions.NotAccept, // 521
			AcceptConditions.Accept, // 522
			AcceptConditions.NotAccept, // 523
			AcceptConditions.Accept, // 524
			AcceptConditions.NotAccept, // 525
			AcceptConditions.Accept, // 526
			AcceptConditions.NotAccept, // 527
			AcceptConditions.Accept, // 528
			AcceptConditions.NotAccept, // 529
			AcceptConditions.NotAccept, // 530
			AcceptConditions.NotAccept, // 531
			AcceptConditions.NotAccept, // 532
			AcceptConditions.NotAccept, // 533
			AcceptConditions.NotAccept, // 534
			AcceptConditions.NotAccept, // 535
			AcceptConditions.NotAccept, // 536
			AcceptConditions.NotAccept, // 537
			AcceptConditions.NotAccept, // 538
			AcceptConditions.NotAccept, // 539
			AcceptConditions.NotAccept, // 540
			AcceptConditions.NotAccept, // 541
			AcceptConditions.NotAccept, // 542
			AcceptConditions.NotAccept, // 543
			AcceptConditions.NotAccept, // 544
			AcceptConditions.NotAccept, // 545
			AcceptConditions.NotAccept, // 546
			AcceptConditions.NotAccept, // 547
			AcceptConditions.NotAccept, // 548
			AcceptConditions.NotAccept, // 549
			AcceptConditions.NotAccept, // 550
			AcceptConditions.NotAccept, // 551
			AcceptConditions.NotAccept, // 552
			AcceptConditions.NotAccept, // 553
			AcceptConditions.NotAccept, // 554
			AcceptConditions.NotAccept, // 555
			AcceptConditions.NotAccept, // 556
			AcceptConditions.NotAccept, // 557
			AcceptConditions.NotAccept, // 558
			AcceptConditions.NotAccept, // 559
			AcceptConditions.NotAccept, // 560
			AcceptConditions.NotAccept, // 561
			AcceptConditions.NotAccept, // 562
			AcceptConditions.NotAccept, // 563
			AcceptConditions.NotAccept, // 564
			AcceptConditions.NotAccept, // 565
			AcceptConditions.NotAccept, // 566
			AcceptConditions.NotAccept, // 567
			AcceptConditions.NotAccept, // 568
			AcceptConditions.NotAccept, // 569
			AcceptConditions.NotAccept, // 570
			AcceptConditions.NotAccept, // 571
			AcceptConditions.NotAccept, // 572
			AcceptConditions.NotAccept, // 573
			AcceptConditions.NotAccept, // 574
			AcceptConditions.NotAccept, // 575
			AcceptConditions.NotAccept, // 576
			AcceptConditions.NotAccept, // 577
			AcceptConditions.NotAccept, // 578
			AcceptConditions.NotAccept, // 579
			AcceptConditions.NotAccept, // 580
			AcceptConditions.NotAccept, // 581
			AcceptConditions.NotAccept, // 582
			AcceptConditions.NotAccept, // 583
			AcceptConditions.NotAccept, // 584
			AcceptConditions.NotAccept, // 585
			AcceptConditions.NotAccept, // 586
			AcceptConditions.NotAccept, // 587
			AcceptConditions.NotAccept, // 588
			AcceptConditions.NotAccept, // 589
			AcceptConditions.Accept, // 590
			AcceptConditions.Accept, // 591
			AcceptConditions.Accept, // 592
			AcceptConditions.Accept, // 593
			AcceptConditions.Accept, // 594
			AcceptConditions.Accept, // 595
			AcceptConditions.Accept, // 596
			AcceptConditions.NotAccept, // 597
			AcceptConditions.NotAccept, // 598
			AcceptConditions.NotAccept, // 599
			AcceptConditions.NotAccept, // 600
			AcceptConditions.NotAccept, // 601
			AcceptConditions.NotAccept, // 602
			AcceptConditions.NotAccept, // 603
			AcceptConditions.NotAccept, // 604
			AcceptConditions.NotAccept, // 605
			AcceptConditions.NotAccept, // 606
			AcceptConditions.NotAccept, // 607
			AcceptConditions.NotAccept, // 608
			AcceptConditions.NotAccept, // 609
			AcceptConditions.NotAccept, // 610
			AcceptConditions.NotAccept, // 611
			AcceptConditions.NotAccept, // 612
			AcceptConditions.NotAccept, // 613
			AcceptConditions.NotAccept, // 614
			AcceptConditions.NotAccept, // 615
			AcceptConditions.NotAccept, // 616
			AcceptConditions.Accept, // 617
			AcceptConditions.NotAccept, // 618
			AcceptConditions.Accept, // 619
			AcceptConditions.Accept, // 620
			AcceptConditions.Accept, // 621
			AcceptConditions.Accept, // 622
			AcceptConditions.Accept, // 623
			AcceptConditions.Accept, // 624
			AcceptConditions.Accept, // 625
			AcceptConditions.Accept, // 626
			AcceptConditions.Accept, // 627
			AcceptConditions.Accept, // 628
			AcceptConditions.Accept, // 629
			AcceptConditions.Accept, // 630
			AcceptConditions.Accept, // 631
			AcceptConditions.Accept, // 632
			AcceptConditions.Accept, // 633
			AcceptConditions.Accept, // 634
			AcceptConditions.Accept, // 635
			AcceptConditions.Accept, // 636
			AcceptConditions.Accept, // 637
			AcceptConditions.Accept, // 638
			AcceptConditions.Accept, // 639
			AcceptConditions.Accept, // 640
			AcceptConditions.Accept, // 641
			AcceptConditions.Accept, // 642
			AcceptConditions.Accept, // 643
			AcceptConditions.Accept, // 644
			AcceptConditions.Accept, // 645
			AcceptConditions.Accept, // 646
			AcceptConditions.Accept, // 647
			AcceptConditions.Accept, // 648
			AcceptConditions.Accept, // 649
			AcceptConditions.Accept, // 650
			AcceptConditions.Accept, // 651
			AcceptConditions.Accept, // 652
			AcceptConditions.Accept, // 653
			AcceptConditions.Accept, // 654
			AcceptConditions.Accept, // 655
			AcceptConditions.Accept, // 656
			AcceptConditions.Accept, // 657
			AcceptConditions.Accept, // 658
			AcceptConditions.Accept, // 659
			AcceptConditions.Accept, // 660
			AcceptConditions.Accept, // 661
			AcceptConditions.Accept, // 662
			AcceptConditions.Accept, // 663
			AcceptConditions.Accept, // 664
			AcceptConditions.Accept, // 665
			AcceptConditions.Accept, // 666
			AcceptConditions.Accept, // 667
			AcceptConditions.Accept, // 668
			AcceptConditions.Accept, // 669
			AcceptConditions.Accept, // 670
			AcceptConditions.Accept, // 671
			AcceptConditions.Accept, // 672
			AcceptConditions.Accept, // 673
			AcceptConditions.Accept, // 674
			AcceptConditions.Accept, // 675
			AcceptConditions.Accept, // 676
			AcceptConditions.Accept, // 677
			AcceptConditions.Accept, // 678
			AcceptConditions.Accept, // 679
			AcceptConditions.Accept, // 680
			AcceptConditions.Accept, // 681
			AcceptConditions.Accept, // 682
			AcceptConditions.Accept, // 683
			AcceptConditions.Accept, // 684
			AcceptConditions.Accept, // 685
			AcceptConditions.Accept, // 686
			AcceptConditions.Accept, // 687
			AcceptConditions.Accept, // 688
			AcceptConditions.Accept, // 689
			AcceptConditions.Accept, // 690
			AcceptConditions.Accept, // 691
			AcceptConditions.Accept, // 692
			AcceptConditions.Accept, // 693
			AcceptConditions.Accept, // 694
			AcceptConditions.Accept, // 695
			AcceptConditions.Accept, // 696
			AcceptConditions.Accept, // 697
			AcceptConditions.Accept, // 698
			AcceptConditions.Accept, // 699
			AcceptConditions.Accept, // 700
			AcceptConditions.Accept, // 701
			AcceptConditions.Accept, // 702
			AcceptConditions.Accept, // 703
			AcceptConditions.Accept, // 704
			AcceptConditions.Accept, // 705
			AcceptConditions.Accept, // 706
			AcceptConditions.Accept, // 707
			AcceptConditions.Accept, // 708
			AcceptConditions.Accept, // 709
			AcceptConditions.Accept, // 710
			AcceptConditions.Accept, // 711
			AcceptConditions.Accept, // 712
			AcceptConditions.Accept, // 713
			AcceptConditions.Accept, // 714
			AcceptConditions.Accept, // 715
			AcceptConditions.Accept, // 716
			AcceptConditions.Accept, // 717
			AcceptConditions.Accept, // 718
			AcceptConditions.Accept, // 719
			AcceptConditions.Accept, // 720
			AcceptConditions.Accept, // 721
			AcceptConditions.Accept, // 722
			AcceptConditions.Accept, // 723
			AcceptConditions.Accept, // 724
			AcceptConditions.Accept, // 725
			AcceptConditions.Accept, // 726
			AcceptConditions.Accept, // 727
			AcceptConditions.Accept, // 728
			AcceptConditions.Accept, // 729
			AcceptConditions.Accept, // 730
			AcceptConditions.Accept, // 731
			AcceptConditions.Accept, // 732
			AcceptConditions.Accept, // 733
			AcceptConditions.Accept, // 734
			AcceptConditions.Accept, // 735
			AcceptConditions.Accept, // 736
			AcceptConditions.Accept, // 737
			AcceptConditions.Accept, // 738
			AcceptConditions.Accept, // 739
			AcceptConditions.Accept, // 740
			AcceptConditions.Accept, // 741
			AcceptConditions.Accept, // 742
			AcceptConditions.Accept, // 743
			AcceptConditions.Accept, // 744
			AcceptConditions.Accept, // 745
			AcceptConditions.Accept, // 746
			AcceptConditions.Accept, // 747
			AcceptConditions.Accept, // 748
			AcceptConditions.Accept, // 749
			AcceptConditions.Accept, // 750
			AcceptConditions.Accept, // 751
			AcceptConditions.Accept, // 752
			AcceptConditions.Accept, // 753
			AcceptConditions.Accept, // 754
			AcceptConditions.Accept, // 755
			AcceptConditions.Accept, // 756
			AcceptConditions.Accept, // 757
			AcceptConditions.Accept, // 758
			AcceptConditions.Accept, // 759
			AcceptConditions.Accept, // 760
			AcceptConditions.Accept, // 761
			AcceptConditions.Accept, // 762
			AcceptConditions.Accept, // 763
			AcceptConditions.Accept, // 764
			AcceptConditions.Accept, // 765
			AcceptConditions.Accept, // 766
			AcceptConditions.Accept, // 767
			AcceptConditions.Accept, // 768
			AcceptConditions.Accept, // 769
			AcceptConditions.Accept, // 770
			AcceptConditions.Accept, // 771
			AcceptConditions.Accept, // 772
			AcceptConditions.Accept, // 773
			AcceptConditions.Accept, // 774
			AcceptConditions.Accept, // 775
			AcceptConditions.Accept, // 776
			AcceptConditions.Accept, // 777
			AcceptConditions.Accept, // 778
			AcceptConditions.Accept, // 779
			AcceptConditions.Accept, // 780
			AcceptConditions.Accept, // 781
			AcceptConditions.Accept, // 782
			AcceptConditions.Accept, // 783
			AcceptConditions.Accept, // 784
			AcceptConditions.Accept, // 785
			AcceptConditions.Accept, // 786
			AcceptConditions.Accept, // 787
			AcceptConditions.NotAccept, // 788
			AcceptConditions.NotAccept, // 789
			AcceptConditions.Accept, // 790
			AcceptConditions.Accept, // 791
			AcceptConditions.Accept, // 792
			AcceptConditions.Accept, // 793
			AcceptConditions.Accept, // 794
			AcceptConditions.Accept, // 795
			AcceptConditions.Accept, // 796
			AcceptConditions.Accept, // 797
			AcceptConditions.Accept, // 798
			AcceptConditions.Accept, // 799
			AcceptConditions.Accept, // 800
			AcceptConditions.Accept, // 801
			AcceptConditions.Accept, // 802
			AcceptConditions.Accept, // 803
			AcceptConditions.Accept, // 804
			AcceptConditions.Accept, // 805
			AcceptConditions.Accept, // 806
			AcceptConditions.Accept, // 807
			AcceptConditions.Accept, // 808
			AcceptConditions.Accept, // 809
			AcceptConditions.Accept, // 810
			AcceptConditions.Accept, // 811
			AcceptConditions.Accept, // 812
			AcceptConditions.Accept, // 813
			AcceptConditions.Accept, // 814
			AcceptConditions.Accept, // 815
			AcceptConditions.Accept, // 816
			AcceptConditions.Accept, // 817
			AcceptConditions.Accept, // 818
			AcceptConditions.Accept, // 819
			AcceptConditions.Accept, // 820
			AcceptConditions.Accept, // 821
			AcceptConditions.Accept, // 822
			AcceptConditions.Accept, // 823
			AcceptConditions.Accept, // 824
			AcceptConditions.Accept, // 825
			AcceptConditions.Accept, // 826
			AcceptConditions.Accept, // 827
			AcceptConditions.Accept, // 828
			AcceptConditions.Accept, // 829
			AcceptConditions.Accept, // 830
			AcceptConditions.Accept, // 831
			AcceptConditions.Accept, // 832
			AcceptConditions.Accept, // 833
			AcceptConditions.Accept, // 834
			AcceptConditions.Accept, // 835
			AcceptConditions.Accept, // 836
			AcceptConditions.Accept, // 837
			AcceptConditions.Accept, // 838
			AcceptConditions.Accept, // 839
			AcceptConditions.Accept, // 840
			AcceptConditions.Accept, // 841
			AcceptConditions.Accept, // 842
			AcceptConditions.Accept, // 843
			AcceptConditions.Accept, // 844
			AcceptConditions.Accept, // 845
			AcceptConditions.Accept, // 846
			AcceptConditions.Accept, // 847
			AcceptConditions.Accept, // 848
			AcceptConditions.Accept, // 849
			AcceptConditions.Accept, // 850
			AcceptConditions.Accept, // 851
			AcceptConditions.Accept, // 852
			AcceptConditions.Accept, // 853
			AcceptConditions.Accept, // 854
			AcceptConditions.Accept, // 855
			AcceptConditions.Accept, // 856
			AcceptConditions.Accept, // 857
			AcceptConditions.Accept, // 858
			AcceptConditions.Accept, // 859
			AcceptConditions.Accept, // 860
			AcceptConditions.Accept, // 861
			AcceptConditions.Accept, // 862
			AcceptConditions.Accept, // 863
			AcceptConditions.Accept, // 864
			AcceptConditions.Accept, // 865
			AcceptConditions.Accept, // 866
			AcceptConditions.Accept, // 867
			AcceptConditions.Accept, // 868
			AcceptConditions.Accept, // 869
			AcceptConditions.Accept, // 870
			AcceptConditions.Accept, // 871
			AcceptConditions.Accept, // 872
			AcceptConditions.Accept, // 873
			AcceptConditions.Accept, // 874
			AcceptConditions.Accept, // 875
			AcceptConditions.Accept, // 876
			AcceptConditions.Accept, // 877
			AcceptConditions.Accept, // 878
			AcceptConditions.Accept, // 879
			AcceptConditions.Accept, // 880
			AcceptConditions.Accept, // 881
			AcceptConditions.Accept, // 882
			AcceptConditions.Accept, // 883
			AcceptConditions.Accept, // 884
			AcceptConditions.Accept, // 885
			AcceptConditions.Accept, // 886
			AcceptConditions.Accept, // 887
			AcceptConditions.Accept, // 888
			AcceptConditions.NotAccept, // 889
			AcceptConditions.Accept, // 890
			AcceptConditions.Accept, // 891
			AcceptConditions.Accept, // 892
			AcceptConditions.Accept, // 893
			AcceptConditions.Accept, // 894
			AcceptConditions.Accept, // 895
			AcceptConditions.Accept, // 896
			AcceptConditions.Accept, // 897
			AcceptConditions.Accept, // 898
			AcceptConditions.Accept, // 899
			AcceptConditions.Accept, // 900
			AcceptConditions.Accept, // 901
			AcceptConditions.Accept, // 902
		};
		
		private static int[] colMap = new int[]
		{
			64, 64, 64, 64, 64, 64, 64, 64, 64, 23, 11, 64, 64, 24, 64, 64, 
			64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 64, 
			69, 44, 20, 56, 50, 1, 47, 21, 57, 59, 45, 42, 53, 43, 46, 25, 
			55, 60, 62, 61, 39, 68, 38, 68, 58, 52, 41, 66, 2, 18, 22, 5, 
			53, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 4, 10, 16, 33, 30, 27, 37, 51, 70, 54, 70, 49, 34, 
			3, 13, 31, 6, 26, 17, 28, 15, 19, 8, 40, 32, 12, 36, 14, 29, 
			9, 35, 7, 4, 10, 16, 33, 30, 27, 37, 51, 63, 48, 65, 53, 64, 
			67, 0
		};
		
		private static int[] rowMap = new int[]
		{
			0, 1, 2, 3, 4, 1, 1, 5, 1, 6, 7, 8, 9, 10, 1, 11, 
			1, 1, 1, 1, 12, 13, 1, 1, 1, 14, 15, 16, 17, 18, 1, 1, 
			1, 1, 19, 1, 1, 20, 21, 22, 16, 23, 1, 1, 1, 1, 1, 1, 
			1, 1, 1, 1, 1, 1, 1, 1, 24, 1, 25, 1, 16, 16, 16, 16, 
			1, 1, 16, 16, 26, 16, 27, 1, 28, 16, 16, 1, 16, 16, 16, 16, 
			16, 29, 16, 16, 30, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 
			16, 16, 16, 16, 16, 16, 16, 16, 31, 16, 16, 32, 1, 1, 1, 1, 
			33, 34, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 
			1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 1, 1, 1, 1, 1, 16, 
			16, 16, 16, 16, 16, 16, 1, 16, 16, 16, 16, 16, 16, 16, 16, 16, 
			16, 16, 16, 16, 16, 35, 36, 37, 38, 39, 40, 41, 1, 42, 43, 44, 
			39, 1, 45, 1, 1, 46, 1, 47, 1, 48, 1, 1, 49, 50, 1, 51, 
			1, 52, 53, 54, 55, 56, 51, 1, 57, 1, 1, 1, 58, 1, 59, 60, 
			1, 1, 61, 62, 63, 64, 65, 66, 67, 62, 1, 68, 1, 1, 69, 1, 
			70, 71, 1, 1, 72, 1, 1, 73, 1, 74, 75, 76, 1, 77, 78, 1, 
			79, 80, 1, 1, 81, 82, 83, 1, 84, 85, 86, 87, 1, 88, 1, 89, 
			90, 91, 92, 93, 1, 94, 1, 1, 1, 1, 95, 96, 97, 1, 98, 1, 
			1, 1, 1, 99, 100, 101, 102, 1, 103, 1, 1, 1, 1, 104, 1, 105, 
			106, 107, 108, 109, 110, 111, 112, 1, 113, 1, 114, 1, 115, 116, 117, 118, 
			119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 131, 132, 133, 134, 
			135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 
			151, 152, 153, 154, 1, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 
			166, 167, 168, 169, 170, 171, 172, 173, 174, 175, 176, 177, 178, 179, 180, 8, 
			181, 182, 183, 9, 184, 185, 186, 187, 188, 189, 190, 28, 191, 192, 193, 194, 
			195, 196, 197, 198, 199, 200, 201, 157, 202, 203, 204, 205, 206, 207, 208, 209, 
			210, 211, 212, 213, 214, 215, 216, 217, 218, 219, 220, 30, 221, 222, 223, 27, 
			224, 225, 226, 227, 228, 229, 230, 231, 232, 233, 234, 235, 236, 237, 238, 239, 
			240, 241, 242, 243, 244, 245, 246, 247, 248, 249, 250, 251, 252, 253, 254, 255, 
			256, 257, 258, 259, 260, 261, 262, 263, 264, 265, 266, 267, 268, 269, 270, 271, 
			272, 273, 274, 275, 276, 277, 278, 279, 280, 281, 282, 283, 284, 285, 286, 287, 
			288, 289, 290, 291, 292, 293, 294, 295, 296, 297, 298, 299, 300, 301, 302, 303, 
			304, 305, 306, 307, 308, 309, 310, 311, 312, 313, 314, 315, 316, 317, 318, 319, 
			320, 321, 322, 323, 324, 325, 326, 327, 328, 329, 330, 331, 332, 333, 334, 335, 
			336, 337, 338, 339, 340, 341, 342, 343, 344, 345, 346, 347, 348, 35, 349, 350, 
			351, 352, 353, 354, 355, 356, 357, 358, 359, 360, 112, 361, 362, 363, 364, 365, 
			113, 366, 367, 368, 114, 369, 370, 371, 372, 373, 374, 375, 376, 377, 378, 379, 
			380, 381, 382, 383, 384, 385, 386, 387, 217, 388, 389, 390, 391, 392, 393, 394, 
			395, 396, 397, 398, 399, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 
			411, 412, 413, 414, 415, 416, 417, 418, 419, 420, 421, 422, 423, 424, 425, 426, 
			427, 428, 429, 430, 431, 432, 433, 434, 435, 436, 437, 438, 439, 440, 441, 442, 
			443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 
			459, 460, 461, 462, 463, 464, 465, 466, 467, 468, 469, 470, 471, 472, 473, 474, 
			475, 476, 477, 478, 479, 480, 481, 482, 483, 484, 485, 486, 487, 488, 489, 490, 
			491, 492, 493, 494, 495, 496, 497, 498, 499, 500, 501, 502, 503, 504, 505, 506, 
			507, 508, 509, 510, 511, 512, 513, 514, 515, 516, 517, 518, 519, 520, 521, 522, 
			523, 524, 525, 526, 527, 528, 529, 530, 531, 532, 533, 534, 535, 536, 537, 538, 
			539, 540, 541, 542, 543, 544, 545, 546, 547, 548, 549, 550, 551, 552, 553, 554, 
			555, 556, 557, 558, 559, 560, 561, 562, 563, 564, 565, 566, 567, 568, 569, 570, 
			571, 572, 573, 574, 575, 576, 577, 578, 579, 580, 581, 582, 583, 584, 585, 586, 
			587, 588, 589, 590, 591, 592, 593, 594, 595, 596, 597, 598, 599, 600, 601, 602, 
			603, 604, 605, 606, 607, 608, 609, 610, 611, 612, 613, 614, 615, 616, 617, 618, 
			619, 620, 621, 622, 623, 624, 625, 626, 627, 628, 629, 630, 631, 632, 633, 634, 
			635, 636, 637, 638, 639, 640, 641, 642, 643, 644, 645, 646, 647, 648, 649, 650, 
			651, 652, 653, 654, 655, 656, 657, 658, 659, 660, 661, 662, 663, 664, 665, 666, 
			544, 667, 16, 668, 669, 670, 671, 672, 673, 674, 675, 676, 677, 678, 679, 680, 
			681, 682, 683, 684, 685, 686, 687
		};
		
		private static int[,] nextState = new int[,]
		{
			{ 1, 2, 262, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 2, 260, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 316, -1, -1, -1, -1, -1, -1, -1, -1, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 19, -1, -1, -1, 20, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 885, -1, 882, 882, 882, 882, 882, 623, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 624, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 10, -1 },
			{ -1, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 31, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, -1, 383, 383, 383, 385, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, -1, 383, 383, 383 },
			{ -1, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 32, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 389, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, -1, 387, 387, 387 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 397, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 308, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 417, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 57, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 309, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 854, -1, 832, 882, 882, 882, 58, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 797, -1, 833, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 64, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 65, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 427, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 800, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 873, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, 56, -1, -1, 56, -1, -1, 56, -1, 56, 56, 56, -1, -1, -1, -1, -1, 56, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 691, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 388, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 704, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, -1, -1, -1, 72, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, 72, -1, -1, 72, -1, -1, -1, -1, -1, -1, 72, 72, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 72, -1, -1, 72, -1, -1, 72, -1, 72, 72, 72, -1, -1, -1, -1, -1, 72, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 438, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, 84, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 84, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 758, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 821, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 764, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 892, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, -1, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, 181, -1, 181, 181, 181 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, 182, -1, -1, 182, -1, 182, 182, 182, -1, -1, -1, -1, -1, 182, 272, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 184, -1 },
			{ -1, 185, 185, -1, -1, 185, -1, -1, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, 185, 185, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, -1, -1, 185, -1, -1, 185, 185, -1, 185, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, -1 },
			{ -1, -1, -1, 186, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 187, -1, 187, 187, 187, 187, 187, -1, 187, 187, 187, 187, 187, 187, -1, 187, -1, -1, -1, -1, -1, -1, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 187, 187, -1, -1, 187, -1, -1, 187, -1, 187, 187, 187, -1, -1, -1, -1, -1, 187, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, -1, -1, 189, -1, 189, 189, 189, -1, -1, -1, -1, -1, 189, -1, -1 },
			{ -1, 193, 193, 193, 194, 193, 194, 194, 194, 194, 194, 193, 194, 194, 194, 194, 194, 194, 193, 194, 193, 193, 193, 193, 193, 193, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 193, 193, 194, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 194, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 193, 195, 193, 193, 193, -1, 193, 193, 193 },
			{ -1, 196, 196, 196, 196, 196, 197, 198, 196, 196, 198, 196, 196, 196, 198, 196, 199, 196, 196, 196, 200, 196, 196, 196, 196, 196, 196, 277, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 201, 201, 196, 196, 196, 196, 196, 196, 196, 196, 196, 196, 198, 196, 196, 196, 198, 201, 196, 196, 196, 196, 201, 201, 201, 202, 196, 196, 196, -1, 201, 196, 196 },
			{ -1, -1, -1, -1, 194, -1, 194, 194, 194, 194, 194, -1, 194, 194, 194, 194, 194, 194, -1, 194, -1, -1, -1, -1, -1, -1, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, 194, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 194, 194, -1, -1, 194, -1, -1, 194, -1, 194, 194, 194, -1, -1, -1, -1, -1, 194, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 571, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 900, -1, -1, -1, -1, -1, -1, 900, -1, -1, -1, 900, -1, -1, -1, -1, -1, -1, -1, -1, 900, -1, 900, -1, -1, 900, -1, -1, -1, -1, -1, -1, 900, 900, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 900, -1, -1, 900, -1, -1, 900, -1, 900, 900, 900, -1, -1, -1, -1, -1, 900, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, 592, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 592, -1, -1, -1, -1, 592, 592, 592, -1, -1, -1, -1, -1, 592, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 281, -1, -1, -1, -1, -1, -1, 281, -1, -1, -1, 281, -1, -1, -1, -1, -1, -1, -1, -1, 281, -1, 281, -1, -1, 281, -1, -1, -1, -1, -1, -1, 281, 281, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 281, -1, -1, 281, -1, -1, 281, -1, 281, 281, 281, -1, -1, -1, -1, -1, 281, -1, -1 },
			{ -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 574, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, 205, 205, 205 },
			{ -1, 207, 207, -1, -1, 207, -1, -1, -1, -1, -1, 207, -1, -1, -1, -1, -1, -1, 207, -1, -1, 207, 207, 207, 207, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 207, 207, 207, -1, -1, -1, 207, -1, -1, 207, 207, -1, 207, -1, -1, -1, -1, -1, -1, 207, -1, -1, 207, -1 },
			{ -1, -1, -1, -1, 209, -1, 209, 209, 209, 209, 209, -1, 209, 209, 209, 209, 209, 209, -1, 209, -1, -1, -1, -1, -1, -1, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 209, 209, -1, -1, 209, -1, -1, 209, -1, 209, 209, 209, -1, -1, -1, -1, -1, 209, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 210, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, -1, 211, -1, -1, 211, -1, 211, 211, 211, -1, -1, -1, -1, -1, 211, -1, -1 },
			{ -1, 215, 215, 215, 216, 215, 216, 216, 216, 216, 216, 215, 216, 216, 216, 216, 216, 216, 215, 216, 215, 215, 215, 215, 215, 215, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 215, 215, 216, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 216, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 215, 217, 215, 215, 215, -1, 215, 215, 215 },
			{ -1, 218, 218, 219, 218, 218, 220, 221, 218, 218, 221, 218, 218, 218, 221, 218, 222, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 286, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 223, 223, 218, 218, 218, 218, 218, 218, 218, 218, 218, 218, 221, 218, 218, 218, 221, 223, 218, 218, 218, 218, 223, 223, 223, 224, 218, 218, 218, -1, 223, 218, 218 },
			{ -1, -1, -1, -1, 216, -1, 216, 216, 216, 216, 216, -1, 216, 216, 216, 216, 216, 216, -1, 216, -1, -1, -1, -1, -1, -1, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, 216, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 216, 216, -1, -1, 216, -1, -1, 216, -1, 216, 216, 216, -1, -1, -1, -1, -1, 216, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 577, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 901, -1, -1, -1, -1, -1, -1, 901, -1, -1, -1, 901, -1, -1, -1, -1, -1, -1, -1, -1, 901, -1, 901, -1, -1, 901, -1, -1, -1, -1, -1, -1, 901, 901, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 901, -1, -1, 901, -1, -1, 901, -1, 901, 901, 901, -1, -1, -1, -1, -1, 901, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, 594, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 594, -1, -1, -1, -1, 594, 594, 594, -1, -1, -1, -1, -1, 594, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 290, -1, -1, -1, -1, -1, -1, 290, -1, -1, -1, 290, -1, -1, -1, -1, -1, -1, -1, -1, 290, -1, 290, -1, -1, 290, -1, -1, -1, -1, -1, -1, 290, 290, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 290, -1, -1, 290, -1, -1, 290, -1, 290, 290, 290, -1, -1, -1, -1, -1, 290, -1, -1 },
			{ -1, 227, 227, -1, -1, 227, -1, -1, -1, -1, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, -1, 227, 227, 227, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, -1, -1, -1, 227, -1, -1, 227, 227, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, -1, 227, -1 },
			{ -1, -1, -1, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 228, 228, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 229, -1, 229, 229, 229, 229, 229, -1, 229, 229, 229, 229, 229, 229, -1, 229, -1, -1, -1, -1, -1, -1, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 229, 229, -1, -1, 229, -1, -1, 229, -1, 229, 229, 229, -1, -1, -1, -1, -1, 229, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, -1, -1, 230, -1, -1, 230, -1, 230, 230, 230, -1, -1, -1, -1, -1, 230, -1, -1 },
			{ -1, 234, 234, 234, 235, 234, 235, 235, 235, 235, 235, 234, 235, 235, 235, 235, 235, 235, 234, 235, 234, 234, 234, 234, 234, 234, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 234, 234, 235, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 235, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 234, 236, 234, 234, 234, -1, 234, 234, 234 },
			{ -1, 237, 237, 237, 237, 237, 238, 239, 237, 237, 239, 237, 237, 237, 239, 237, 240, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 296, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 241, 241, 237, 237, 237, 237, 237, 237, 237, 237, 237, 237, 239, 237, 237, 237, 239, 241, 237, 237, 237, 237, 241, 241, 241, 242, 237, 237, 237, -1, 241, 237, 237 },
			{ -1, -1, -1, -1, 235, -1, 235, 235, 235, 235, 235, -1, 235, 235, 235, 235, 235, 235, -1, 235, -1, -1, -1, -1, -1, -1, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, 235, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 235, 235, -1, -1, 235, -1, -1, 235, -1, 235, 235, 235, -1, -1, -1, -1, -1, 235, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 582, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 902, -1, -1, -1, -1, -1, -1, 902, -1, -1, -1, 902, -1, -1, -1, -1, -1, -1, -1, -1, 902, -1, 902, -1, -1, 902, -1, -1, -1, -1, -1, -1, 902, 902, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 902, -1, -1, 902, -1, -1, 902, -1, 902, 902, 902, -1, -1, -1, -1, -1, 902, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, 596, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 596, -1, -1, -1, -1, 596, 596, 596, -1, -1, -1, -1, -1, 596, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, -1, -1, -1, 300, -1, -1, -1, 300, -1, -1, -1, -1, -1, -1, -1, -1, 300, -1, 300, -1, -1, 300, -1, -1, -1, -1, -1, -1, 300, 300, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 300, -1, -1, 300, -1, -1, 300, -1, 300, 300, 300, -1, -1, -1, -1, -1, 300, -1, -1 },
			{ -1, -1, -1, -1, 247, -1, 247, 247, 247, 247, 247, -1, 247, 247, 247, 247, 247, 247, -1, 247, -1, -1, -1, -1, -1, -1, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 247, 247, -1, -1, 247, -1, -1, 247, -1, 247, 247, 247, -1, -1, -1, -1, -1, 247, -1, -1 },
			{ -1, -1, -1, -1, 249, -1, 249, 249, 249, 249, 249, -1, 249, 249, 249, 249, 249, 249, -1, 249, -1, -1, -1, -1, -1, -1, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 249, 249, -1, -1, 249, -1, -1, 249, -1, 249, 249, 249, -1, -1, -1, -1, -1, 249, -1, -1 },
			{ -1, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, -1, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, -1, 250, 250, 250 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 252, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, -1, 253, 253, 253 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 255, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 259, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 257, 257, 257, -1, 257, 257, 257, 257, 257, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, -1, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, -1, 257, 257, 257 },
			{ -1, -1, -1, 2, -1, -1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, 581, -1, 581, 581, 581, 581, 581, -1, 581, 581, 581, 581, 581, 581, -1, 581, -1, -1, -1, -1, -1, -1, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, 3, -1, 2, 305, 4, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, -1, 2, 2, 2 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 21, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 22, -1, -1, -1, 23, -1, -1, 379, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 24, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 629, -1, 882, 882, 882, 882, 882, -1, 882, 882, 26, 882, 882, 882, -1, 882, -1, 381, -1, -1, -1, -1, 882, 882, 27, 882, 882, 882, 882, 882, 882, 882, 630, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 393, -1, -1, -1, -1, -1, -1, -1, -1, -1, 395, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 13, 13, -1, -1, -1, -1, -1, -1, 41, -1, -1, -1, -1, -1, 13, -1, -1, 13, -1, -1, 13, -1, 13, 13, 13, -1, -1, -1, -1, -1, 13, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 71, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 87, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 272, -1 },
			{ -1, 185, 185, -1, -1, 185, -1, -1, -1, -1, -1, 185, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, 192, 185, 185, 185, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 185, 185, 185, 185, 185, 185, 185, 185, 185, -1, -1, -1, 185, -1, -1, 185, 185, -1, 185, -1, -1, -1, -1, -1, -1, 185, -1, -1, 185, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 570, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, 189, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 189, -1, -1, 189, -1, -1, 189, -1, 189, 189, 189, -1, -1, -1, -1, -1, 189, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 203, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 204, -1, -1, -1, -1, -1, -1, 204, -1, -1, -1, 204, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, 204, -1, -1, 204, -1, -1, -1, -1, -1, -1, 204, 204, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 204, -1, -1, 204, -1, -1, 204, -1, 204, 204, 204, -1, -1, -1, -1, -1, 204, -1, -1 },
			{ -1, 207, 207, -1, -1, 207, -1, -1, -1, -1, -1, 207, -1, -1, -1, -1, -1, -1, 207, -1, -1, 207, 214, 207, 207, 207, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 207, 207, 207, 207, 207, 207, 207, 207, 207, -1, -1, -1, 207, -1, -1, 207, 207, -1, 207, -1, -1, -1, -1, -1, -1, 207, -1, -1, 207, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 576, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, 211, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 211, -1, -1, 211, -1, -1, 211, -1, 211, 211, 211, -1, -1, -1, -1, -1, 211, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 225, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, 226, -1, -1, -1, 226, -1, -1, -1, -1, -1, -1, -1, -1, 226, -1, 226, -1, -1, 226, -1, -1, -1, -1, -1, -1, 226, 226, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 226, -1, -1, 226, -1, -1, 226, -1, 226, 226, 226, -1, -1, -1, -1, -1, 226, -1, -1 },
			{ -1, 227, 227, -1, -1, 227, -1, -1, -1, -1, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, -1, 227, 233, 227, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, -1, -1, -1, 227, -1, -1, 227, 227, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, -1, 227, -1 },
			{ -1, 227, 227, 228, -1, 227, -1, -1, -1, -1, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, 228, 292, 227, 227, 227, 227, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 227, 227, 227, 227, 227, 227, 227, 227, 227, -1, -1, -1, 227, -1, -1, 227, 227, -1, 227, -1, -1, -1, -1, -1, -1, 227, -1, -1, 227, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 580, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, 230, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 230, -1, -1, 230, -1, -1, 230, -1, 230, 230, 230, -1, -1, -1, -1, -1, 230, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 243, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 244, -1, -1, -1, -1, -1, -1, 244, -1, -1, -1, 244, -1, -1, -1, -1, -1, -1, -1, -1, 244, -1, 244, -1, -1, 244, -1, -1, -1, -1, -1, -1, 244, 244, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 244, -1, -1, 244, -1, -1, 244, -1, 244, 244, 244, -1, -1, -1, -1, -1, 244, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 245, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 258, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 319, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 304, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 28, -1, 882, 830, 882, 882, 882, -1, 882, 882, 330, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 792, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 268, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 270, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, 310, -1, -1, -1, 310, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, 310, -1, -1, 310, -1, -1, -1, -1, -1, -1, 310, 310, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 310, -1, -1, 310, -1, -1, 310, -1, 310, 310, 310, -1, -1, -1, -1, -1, 310, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, 312, -1, -1, -1, 312, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, 312, -1, -1, 312, -1, -1, -1, -1, -1, -1, 312, 312, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 312, -1, -1, 312, -1, -1, 312, -1, 312, 312, 312, -1, -1, -1, -1, -1, 312, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, 314, -1, -1, -1, 314, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, 314, -1, -1, 314, -1, -1, -1, -1, -1, -1, 314, 314, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 314, -1, -1, 314, -1, -1, 314, -1, 314, 314, 314, -1, -1, -1, -1, -1, 314, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 322, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 29, -1, -1, -1, 30, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 339, 882, 882, -1, 882, 882, 882, 882, 882, 644, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 38, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 597, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 33, -1, -1, -1, 34, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 39, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 647, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 325, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 35, -1, -1, -1, -1, -1, -1, 36, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 37, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, 391, -1, 882, -1, 882, 649, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, 11, 12, -1, -1, -1, -1, 882, 882, 882, 650, 882, 882, 882, 882, 882, 882, 882, 40, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, 264, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 6, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 42, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 43, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 357, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 60, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 331, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 44, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 45, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 61, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 46, -1, -1, -1, 47, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 48, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 62, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 337, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, 334, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 334, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 269, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 63, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 340, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 49, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 66, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 343, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 50, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, 41, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 41, -1, -1, 41, -1, -1, 41, -1, 41, 41, 41, -1, -1, -1, -1, -1, 41, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 67, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 346, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 51, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 52, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 68, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 349, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 53, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 54, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 69, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 352, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 55, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 686, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 73, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 355, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 56, -1, 56, 56, 56, 56, 56, -1, 56, 56, 56, 56, 56, 56, -1, 56, -1, -1, -1, -1, -1, -1, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, 56, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 56, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 74, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 76, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1, -1, -1, -1, -1, -1, 361, -1, -1, -1, -1, 358, 358, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 358, -1 },
			{ -1, -1, -1, -1, 399, -1, -1, 401, 403, -1, -1, -1, -1, 601, -1, -1, 405, -1, -1, -1, -1, -1, -1, 407, -1, -1, 409, -1, 411, 413, -1, 415, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 407, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 77, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 363, -1, 361, -1, -1, -1, -1, -1, -1, -1, -1, 365, 889, -1, 361, 361, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 361, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 78, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 598, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 79, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 367, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 80, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 371, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 81, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 263, 369, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 82, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 373, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 83, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 85, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 86, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ 1, 7, 265, 8, 9, 306, 784, 827, 266, 851, 590, 10, 864, 307, 617, 872, 619, 878, 317, 882, 11, 12, 320, 10, 10, 323, 318, 620, 621, 321, 883, 324, 882, 622, 884, 882, 882, 882, 13, 13, 882, 326, 329, 332, 335, 338, 341, 344, 347, 350, 353, 882, 13, 356, 14, 267, 15, 359, 13, 356, 13, 13, 13, 16, 17, 18, 356, 1, 13, 10, 356 },
			{ -1, -1, -1, -1, 88, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 419, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 89, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 421, 423, 423, 421, 421, 421, 421, 421, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 59, 423, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 425, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 90, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 91, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, 383, -1, 383, 383, 383 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 92, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 93, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, 387, -1, 387, 387, 387 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 94, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, 429, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 95, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 70, 70, -1, -1, 431, 431, -1, -1, -1, -1, -1, -1, -1, -1, 70, -1, -1, 70, -1, -1, 70, -1, 70, 70, 70, -1, -1, -1, -1, -1, 70, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 96, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 97, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 433, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 98, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 435, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 99, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 602, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 100, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 437, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 101, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 439, -1, -1, -1, -1, -1, 441, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 102, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 103, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 443, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 104, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 445, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 105, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 447, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 106, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 604, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 605, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 107, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 449, -1, 449, 449, 449, 449, 449, -1, 449, 449, 449, 449, 449, 449, -1, 449, -1, -1, -1, 417, -1, -1, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 417, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 109, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 789, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 110, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 421, 423, 423, 421, 421, 421, 421, 421, 421, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 75, 423, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 425, 421, 423, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 111, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 59, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 451, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, -1, 423, 423, 423 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 112, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 421, 600, 600, 421, 421, 421, 421, 421, 421, 421, 600, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 600, 421, 453, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, 600, 421, 421, 421, 421, 421, 421, 421, 421, 421, 421, -1, 421, 421, 421 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 113, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 114, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, 417, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 115, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 116, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 455, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 117, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 457, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 118, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 461, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 119, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 465, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 120, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 467, -1, -1, -1, 469, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 121, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 471, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 122, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 608, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 123, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 609, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 128, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 449, -1, 449, 449, 449, 449, 449, 87, 449, 449, 449, 449, 449, 449, -1, 449, -1, -1, -1, -1, 271, -1, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, 449, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 449, 449, -1, -1, 449, -1, -1, 449, -1, 449, 449, 449, -1, -1, -1, -1, -1, 449, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 129, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 453, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, 600, -1, 600, 600, 600 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 130, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 600, 423, 423, 423, 423, 423, 423, 423, 423, 423, 59, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 451, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, 423, -1, 423, 423, 423 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 131, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 473, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 132, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 475, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 133, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 134, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 479, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 483, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, 108, 487, 489, -1, -1, -1, -1, -1, -1, -1, 481, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 135, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 491, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 136, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 493, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 137, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 495, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 138, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 497, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 145, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 499, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 146, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 509, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 147, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 610, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 148, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 124, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 149, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 511, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 150, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 108, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 151, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 152, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 125, -1, -1, -1, -1, -1, -1, -1, -1, -1, 485, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 153, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 159, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 160, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 161, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 521, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, 126, 525, 527, -1, -1, -1, -1, -1, -1, -1, 519, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 162, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 163, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 530, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 164, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 531, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 165, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 533, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 167, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 534, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 168, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 612, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 169, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 613, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 170, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 536, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 171, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 538, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 172, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 139, -1, -1, -1, -1, -1, -1, -1, -1, -1, 513, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 173, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 140, -1, -1, -1, -1, -1, -1, -1, -1, -1, 515, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 174, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 141, -1, -1, -1, -1, -1, -1, -1, -1, -1, 517, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 175, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 126, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 176, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 177, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 142, -1, -1, -1, -1, -1, -1, -1, -1, -1, 523, -1 },
			{ -1, -1, -1, -1, 882, -1, 178, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 179, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 519, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 180, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 143, -1, -1, -1, -1, -1, -1, -1, -1, -1, 529, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 541, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 477, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 144, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 127, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 154, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 481, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 155, -1, -1, -1, -1, -1, -1, -1, -1, -1, 539, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 156, -1, -1, -1, -1, -1, -1, -1, -1, -1, 540, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 157, -1, -1, -1, -1, -1, -1, -1, -1, -1, 542, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 158, -1, -1, -1, -1, -1, -1, -1, -1, -1, 543, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 535, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 25, 545, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 548, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1, -1, 549, -1, 614, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 546, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 166, -1, -1, -1, -1, -1, -1, -1, -1, -1, 547, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 550, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 551, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 553, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 554, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 615, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 555, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 556, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 559, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 558, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, 182, -1, -1, -1, 560, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, 182, -1, -1, 182, -1, 182, 182, 182, -1, -1, -1, -1, -1, 182, 558, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 561, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, 182, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 182, -1, -1, 182, -1, -1, 182, -1, 182, 182, 182, -1, -1, -1, -1, -1, 182, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 562, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 564, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1, -1, -1, -1, 616, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 563, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 565, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 567, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 568, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 183, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 184, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ 1, 185, 185, 186, 187, 185, 187, 187, 187, 187, 187, 185, 187, 187, 187, 187, 187, 187, 185, 187, 188, 185, 185, 185, 185, 185, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 187, 189, 189, 187, 185, 185, 273, 185, 185, 185, 185, 185, 185, 190, 187, 189, 185, 191, 274, 185, 185, 189, 185, 189, 189, 189, 275, 276, 311, 185, 1, 189, 185, 311 },
			{ -1, -1, -1, -1, 572, -1, 572, 572, 572, 572, 572, -1, 572, 572, 572, 572, 572, 572, -1, 572, -1, -1, -1, -1, -1, -1, 572, 572, 572, 572, 572, 572, 572, 572, -1, 572, 572, 572, 572, 572, 572, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, 572, 572, -1, -1, 572, -1, -1, 572, -1, 572, 572, 572, -1, -1, -1, -1, -1, 572, 572, -1 },
			{ -1, -1, -1, -1, 572, -1, 572, 572, 572, 572, 572, -1, 572, 572, 572, 572, 572, 572, -1, 572, -1, -1, -1, -1, -1, -1, 572, 572, 572, 572, 572, 572, 572, 572, -1, 572, 572, 572, 572, 572, 572, -1, -1, 572, -1, -1, -1, -1, -1, -1, -1, 572, 572, -1, -1, 572, -1, -1, 572, -1, 572, 572, 572, -1, -1, 278, -1, -1, 572, 572, -1 },
			{ 1, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 206, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 574, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 1, 205, 205, 205 },
			{ -1, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, 205, -1, 205, 205, 205 },
			{ 1, 207, 207, 208, 209, 207, 209, 209, 209, 209, 209, 207, 209, 209, 209, 209, 209, 209, 207, 209, 210, 207, 207, 207, 207, 207, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 209, 211, 211, 209, 207, 207, 282, 207, 207, 207, 207, 207, 207, 212, 209, 211, 207, 213, 283, 207, 207, 211, 207, 211, 211, 211, 284, 285, 313, 207, 1, 211, 207, 313 },
			{ -1, -1, -1, -1, 578, -1, 578, 578, 578, 578, 578, -1, 578, 578, 578, 578, 578, 578, -1, 578, -1, -1, -1, -1, -1, -1, 578, 578, 578, 578, 578, 578, 578, 578, -1, 578, 578, 578, 578, 578, 578, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, 578, 578, -1, -1, 578, -1, -1, 578, -1, 578, 578, 578, -1, -1, -1, -1, -1, 578, 578, -1 },
			{ -1, -1, -1, -1, 578, -1, 578, 578, 578, 578, 578, -1, 578, 578, 578, 578, 578, 578, -1, 578, -1, -1, -1, -1, -1, -1, 578, 578, 578, 578, 578, 578, 578, 578, -1, 578, 578, 578, 578, 578, 578, -1, -1, 578, -1, -1, -1, -1, -1, -1, -1, 578, 578, -1, -1, 578, -1, -1, 578, -1, 578, 578, 578, -1, -1, 287, -1, -1, 578, 578, -1 },
			{ 1, 227, 227, 228, 229, 227, 229, 229, 229, 229, 229, 227, 229, 229, 229, 229, 229, 229, 227, 229, 228, 292, 227, 227, 227, 227, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 229, 230, 230, 229, 227, 227, 291, 227, 227, 227, 227, 227, 227, 231, 229, 230, 227, 232, 293, 227, 227, 230, 227, 230, 230, 230, 294, 295, 315, 227, 261, 230, 227, 315 },
			{ -1, -1, -1, -1, 581, -1, 581, 581, 581, 581, 581, 245, 581, 581, 581, 581, 581, 581, -1, 581, -1, -1, -1, -1, 301, -1, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, 581, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 581, 581, -1, -1, 581, -1, -1, 581, -1, 581, 581, 581, -1, -1, -1, 583, -1, 581, -1, -1 },
			{ -1, -1, -1, -1, 584, -1, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, 584, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1, 584, 584, -1, -1, 584, -1, -1, 584, -1, 584, 584, 584, -1, -1, -1, -1, -1, 584, 584, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 245, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 301, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 584, -1, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, 584, -1, -1, -1, -1, -1, -1, 584, 584, 584, 584, 584, 584, 584, 584, -1, 584, 584, 584, 584, 584, 584, -1, -1, 584, -1, -1, -1, -1, -1, -1, -1, 584, 584, -1, -1, 584, -1, -1, 584, -1, 584, 584, 584, -1, -1, 297, -1, -1, 584, 584, -1 },
			{ 1, 246, 246, 246, 247, 246, 247, 247, 247, 247, 247, 246, 247, 247, 247, 247, 247, 247, 246, 247, 246, 246, 246, 246, 246, 246, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 247, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 247, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 246, 1, 246, 246, 246 },
			{ 1, 248, 248, 248, 249, 248, 249, 249, 249, 249, 249, 248, 249, 249, 249, 249, 249, 249, 248, 249, 248, 248, 248, 248, 248, 248, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 249, 248, 248, 249, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 249, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 248, 1, 248, 248, 248 },
			{ 1, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 251, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 250, 1, 250, 250, 250 },
			{ 1, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 254, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 253, 1, 253, 253, 253 },
			{ 1, 256, 257, 257, 257, 256, 257, 257, 257, 257, 257, 258, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 302, 257, 303, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 257, 1, 257, 257, 257 },
			{ -1, -1, -1, -1, 882, -1, 882, 327, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 794, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, 279, -1, -1, -1, 279, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, 279, -1, -1, 279, -1, -1, -1, -1, -1, -1, 279, 279, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 279, -1, -1, 279, -1, -1, 279, -1, 279, 279, 279, -1, -1, -1, -1, -1, 279, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, 280, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 280, -1, -1, -1, -1, 280, 280, 280, -1, -1, -1, -1, -1, 280, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, -1, -1, -1, 288, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, 288, -1, -1, 288, -1, -1, -1, -1, -1, -1, 288, 288, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 288, -1, -1, 288, -1, -1, 288, -1, 288, 288, 288, -1, -1, -1, -1, -1, 288, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, 289, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 289, -1, -1, -1, -1, 289, 289, 289, -1, -1, -1, -1, -1, 289, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, 298, -1, -1, -1, 298, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, 298, -1, -1, 298, -1, -1, -1, -1, -1, -1, 298, 298, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 298, -1, -1, 298, -1, -1, 298, -1, 298, 298, 298, -1, -1, -1, -1, -1, 298, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, 299, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 299, -1, -1, -1, -1, 299, 299, 299, -1, -1, -1, -1, -1, 299, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 328, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 369, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 375, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 603, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 459, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 463, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 618, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 607, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 507, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 505, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 611, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 501, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 537, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 532, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 544, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 545, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 552, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 557, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, 566, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 635, 882, 882, 636, 333, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 503, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 336, -1, 882, 882, 882, 882, 882, -1, 882, 882, 829, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 342, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 645, 791, 882, 882, -1, 882, 646, 882, 882, 828, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 345, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 348, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 351, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 798, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 834, 882, 882, 882, -1, 882, 653, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 654, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 354, -1, 882, 882, 882, 882, 655, -1, 796, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 656, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 831, -1, 882, 882, 882, 882, 657, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 799, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 658, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 659, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 660, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 866, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 661, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 852, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 360, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 853, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 362, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 664, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 364, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 366, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 368, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 667, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 370, 882, 865, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 372, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 669, -1, 893, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 670, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 374, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 672, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 879, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 673, 882, 882, -1, 882, 882, 882, 882, 882, 674, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 675, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 376, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 676, -1, 677, 882, 882, 882, 678, -1, 679, 835, 802, 680, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 681, 882, 682, 882, 836, 882, 882, 882, 882, 882, 683, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 685, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 378, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 380, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 382, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 384, -1, 882, 882, 882, 882, 890, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 689, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 386, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 804, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 692, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 390, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 875, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 837, -1, 882, 882, 882, 882, 882, 693, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 392, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 394, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 396, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 698, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 398, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 699, -1, 882, 882, 400, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 857, 882, 700, 882, 701, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 402, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 868, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 805, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 404, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 406, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 408, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 410, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 412, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 876, 882, 882, 882, 882, 414, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 810, 706, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 840, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 808, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 859, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 416, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 858, 882, 882, -1, 882, 882, 882, 882, 882, 894, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 708, 882, 882, -1, 882, 882, 882, 882, 870, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 841, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 418, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 420, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 422, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 424, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 710, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 426, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 713, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 714, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 428, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 430, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 432, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 434, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 718, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 812, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 436, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 719, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 440, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 814, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 442, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 844, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 724, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 444, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 446, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 731, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 817, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 847, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 863, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 734, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 448, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 450, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 737, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 452, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 816, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 454, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 861, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 846, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 456, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 741, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 458, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 460, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 462, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 464, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 466, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 845, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 743, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 744, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 819, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 862, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 468, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 748, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 470, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 472, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 474, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 751, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 476, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 757, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 478, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 759, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 480, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 823, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 482, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 484, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 849, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 486, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 763, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 488, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 765, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 490, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 766, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 492, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 494, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 496, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 498, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 722, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 767, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 768, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 500, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 825, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 772, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 773, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 774, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 502, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 504, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 506, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 508, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 775, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 510, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 512, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 514, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 516, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 778, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 780, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 518, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 781, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 520, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 522, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 524, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 783, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 526, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 528, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 625, 626, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 627, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, -1, -1, -1, 591, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, 591, -1, -1, 591, -1, -1, -1, -1, -1, -1, 591, 591, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 591, -1, -1, 591, -1, -1, 591, -1, 591, 591, 591, -1, -1, -1, -1, -1, 591, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, 593, -1, -1, -1, 593, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, 593, -1, -1, 593, -1, -1, -1, -1, -1, -1, 593, 593, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 593, -1, -1, 593, -1, -1, 593, -1, 593, 593, 593, -1, -1, -1, -1, -1, 593, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, 595, -1, -1, -1, 595, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, 595, -1, -1, 595, -1, -1, -1, -1, -1, -1, 595, 595, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 595, -1, -1, 595, -1, -1, 595, -1, 595, 595, 595, -1, -1, -1, -1, -1, 595, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 599, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, 606, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 684, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 671, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 874, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 668, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 662, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 665, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 856, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 695, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 687, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 803, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 702, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 703, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 707, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 712, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 811, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 723, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 842, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 720, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 728, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 730, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 727, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 871, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 818, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 739, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 740, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 895, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 753, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 745, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 755, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 822, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 762, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 769, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 771, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 770, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 776, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 777, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 779, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 628, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 801, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 666, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 663, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 855, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 690, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 696, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 688, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 809, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 709, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 717, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 716, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 725, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 815, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 733, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 738, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 746, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 742, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 760, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 756, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 747, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 824, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 896, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 782, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 631, 882, 882, 882, -1, 882, 632, 882, 882, 633, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 694, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 697, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 867, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 869, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 711, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 813, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 732, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 729, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 750, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 754, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 761, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 749, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 634, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 807, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 838, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 881, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 721, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 736, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 843, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 752, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 637, 882, 882, 882, -1, 795, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 638, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 839, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 806, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 715, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 726, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 735, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 639, 882, 882, 882, 882, -1, 640, 882, 641, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 642, 882, 882, 882, 882, 882, 643, 882, 882, 793, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 705, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 860, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 648, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 651, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 790, 882, 882, 882, -1, 882, 652, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 785, -1, -1, -1, -1, -1, -1, 785, -1, -1, -1, 785, -1, -1, -1, -1, -1, -1, -1, -1, 785, -1, 785, -1, -1, 785, -1, -1, -1, -1, -1, -1, 785, 785, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 785, -1, -1, 785, -1, -1, 785, -1, 785, 785, 785, -1, -1, -1, -1, -1, 785, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 786, -1, -1, -1, -1, -1, -1, 786, -1, -1, -1, 786, -1, -1, -1, -1, -1, -1, -1, -1, 786, -1, 786, -1, -1, 786, -1, -1, -1, -1, -1, -1, 786, 786, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 786, -1, -1, 786, -1, -1, 786, -1, 786, 786, 786, -1, -1, -1, -1, -1, 786, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 787, -1, -1, -1, -1, -1, -1, 787, -1, -1, -1, 787, -1, -1, -1, -1, -1, -1, -1, -1, 787, -1, 787, -1, -1, 787, -1, -1, -1, -1, -1, -1, 787, 787, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 787, -1, -1, 787, -1, -1, 787, -1, 787, 787, 787, -1, -1, -1, -1, -1, 787, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, -1, -1, -1, 788, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 877, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 820, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 826, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 880, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 891, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 848, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, 882, -1, 882, 882, 882, 882, 882, -1, 882, 882, 882, 882, 882, 882, -1, 882, -1, -1, -1, -1, -1, -1, 882, 882, 882, 850, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, 882, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 882, 882, -1, -1, 882, -1, -1, 882, -1, 882, 882, 882, -1, -1, -1, -1, -1, 882, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 886, -1, -1, -1, -1, -1, -1, 886, -1, -1, -1, 886, -1, -1, -1, -1, -1, -1, -1, -1, 886, -1, 886, -1, -1, 886, -1, -1, -1, -1, -1, -1, 886, 886, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 886, -1, -1, 886, -1, -1, 886, -1, 886, 886, 886, -1, -1, -1, -1, -1, 886, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 887, -1, -1, -1, -1, -1, -1, 887, -1, -1, -1, 887, -1, -1, -1, -1, -1, -1, -1, -1, 887, -1, 887, -1, -1, 887, -1, -1, -1, -1, -1, -1, 887, 887, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 887, -1, -1, 887, -1, -1, 887, -1, 887, 887, 887, -1, -1, -1, -1, -1, 887, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 888, -1, -1, -1, -1, -1, -1, 888, -1, -1, -1, 888, -1, -1, -1, -1, -1, -1, -1, -1, 888, -1, 888, -1, -1, 888, -1, -1, -1, -1, -1, -1, 888, 888, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 888, -1, -1, 888, -1, -1, 888, -1, 888, 888, 888, -1, -1, -1, -1, -1, 888, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 897, -1, -1, -1, -1, -1, -1, 897, -1, -1, -1, 897, -1, -1, -1, -1, -1, -1, -1, -1, 897, -1, 897, -1, -1, 897, -1, -1, -1, -1, -1, -1, 897, 897, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 897, -1, -1, 897, -1, -1, 897, -1, 897, 897, 897, -1, -1, -1, -1, -1, 897, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 898, -1, -1, -1, -1, -1, -1, 898, -1, -1, -1, 898, -1, -1, -1, -1, -1, -1, -1, -1, 898, -1, 898, -1, -1, 898, -1, -1, -1, -1, -1, -1, 898, 898, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 898, -1, -1, 898, -1, -1, 898, -1, 898, 898, 898, -1, -1, -1, -1, -1, 898, -1, -1 },
			{ -1, -1, -1, -1, -1, -1, 899, -1, -1, -1, -1, -1, -1, 899, -1, -1, -1, 899, -1, -1, -1, -1, -1, -1, -1, -1, 899, -1, 899, -1, -1, 899, -1, -1, -1, -1, -1, -1, 899, 899, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, 899, -1, -1, 899, -1, -1, 899, -1, 899, 899, 899, -1, -1, -1, -1, -1, 899, -1, -1 }
		};
		
		
		private static int[] yy_state_dtrans = new int[]
		{
			  0,
			  377,
			  569,
			  573,
			  575,
			  579,
			  585,
			  586,
			  587,
			  588,
			  589
		};
		
		#endregion
		
		public Tokens GetNextToken()
		{
			int current_state = yy_state_dtrans[(int)current_lexical_state];
			int last_accept_state = NoState;
			bool is_initial_state = true;
			
			MarkTokenChunkStart();
			token_start = token_chunk_start;
			expanding_token = false;
			AdvanceEndPosition((token_end > 0) ? token_end - 1 : 0, token_start);
			
			// capture token start position:
			token_start_pos.Char = token_end_pos.Char;
			token_start_pos.Line = token_end_pos.Line;
			token_start_pos.Column = token_end_pos.Column;
			
			if (acceptCondition[current_state] != AcceptConditions.NotAccept)
			{
				last_accept_state = current_state;
				MarkTokenEnd();
			}
			
			while (true)
			{
				char lookahead = (is_initial_state && yy_at_bol) ? BOL : Advance();
				int next_state = nextState[rowMap[current_state], colMap[lookahead]];
				
				if (lookahead == EOF && is_initial_state)
				{
					return Tokens.EOF;
				}
				if (next_state != -1)
				{
					current_state = next_state;
					is_initial_state = false;
					
					if (acceptCondition[current_state] != AcceptConditions.NotAccept)
					{
						last_accept_state = current_state;
						MarkTokenEnd();
					}
				}
				else
				{
					if (last_accept_state == NoState)
					{
						return Tokens.ERROR;
					}
					else
					{
						if ((acceptCondition[last_accept_state] & AcceptConditions.AcceptOnEnd) != 0)
							TrimTokenEnd();
						MoveToTokenEnd();
						
						if (last_accept_state < 0)
						{
							System.Diagnostics.Debug.Assert(last_accept_state >= 903);
						}
						else
						{
							bool accepted = false;
							yyreturn = Accept0(last_accept_state, out accepted);
							if (accepted)
							{
								AdvanceEndPosition(token_start, token_end - 1);
								return yyreturn;
							}
						}
						
						// token ignored:
						is_initial_state = true;
						current_state = yy_state_dtrans[(int)current_lexical_state];
						last_accept_state = NoState;
						MarkTokenChunkStart();
						if (acceptCondition[current_state] != AcceptConditions.NotAccept)
						{
							last_accept_state = current_state;
							MarkTokenEnd();
						}
					}
				}
			}
		} // end of GetNextToken
	}
}

