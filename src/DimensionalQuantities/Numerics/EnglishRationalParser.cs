using System.Numerics;

namespace DimensionalQuantities.Numerics;

internal static class EnglishRationalParser
{
	private static readonly Dictionary<string, Token> _vocabulary = new(StringComparer.Ordinal)
	{
		["Zero"] = new(TokenKind.Number, 0),
		["One"] = new(TokenKind.Number, 1),
		["Two"] = new(TokenKind.Number, 2),
		["Three"] = new(TokenKind.Number, 3),
		["Four"] = new(TokenKind.Number, 4),
		["Five"] = new(TokenKind.Number, 5),
		["Six"] = new(TokenKind.Number, 6),
		["Seven"] = new(TokenKind.Number, 7),
		["Eight"] = new(TokenKind.Number, 8),
		["Nine"] = new(TokenKind.Number, 9),
		["Ten"] = new(TokenKind.Number, 10),
		["Eleven"] = new(TokenKind.Number, 11),
		["Twelve"] = new(TokenKind.Number, 12),
		["Thirteen"] = new(TokenKind.Number, 13),
		["Fourteen"] = new(TokenKind.Number, 14),
		["Fifteen"] = new(TokenKind.Number, 15),
		["Sixteen"] = new(TokenKind.Number, 16),
		["Seventeen"] = new(TokenKind.Number, 17),
		["Eighteen"] = new(TokenKind.Number, 18),
		["Nineteen"] = new(TokenKind.Number, 19),
		["Twenty"] = new(TokenKind.Number, 20),
		["Thirty"] = new(TokenKind.Number, 30),
		["Forty"] = new(TokenKind.Number, 40),
		["Fifty"] = new(TokenKind.Number, 50),
		["Sixty"] = new(TokenKind.Number, 60),
		["Seventy"] = new(TokenKind.Number, 70),
		["Eighty"] = new(TokenKind.Number, 80),
		["Ninety"] = new(TokenKind.Number, 90),
		["Hundred"] = new(TokenKind.Hundred, 100),
		["Thousand"] = new(TokenKind.Scale, BigInteger.Pow(10, 3)),
		["Million"] = new(TokenKind.Scale, BigInteger.Pow(10, 6)),
		["Billion"] = new(TokenKind.Scale, BigInteger.Pow(10, 9)),
		["Trillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 12)),
		["Quadrillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 15)),
		["Quintillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 18)),
		["Sextillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 21)),
		["Septillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 24)),
		["Octillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 27)),
		["Nonillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 30)),
		["Decillion"] = new(TokenKind.Scale, BigInteger.Pow(10, 33)),
		["Minus"] = new(TokenKind.Minus, 0),
		["And"] = new(TokenKind.And, 0),
		["Over"] = new(TokenKind.Over, 0),
		["Half"] = new(TokenKind.SingularDenominator, 2),
		["Halves"] = new(TokenKind.PluralDenominator, 2),
		["Third"] = new(TokenKind.SingularDenominator, 3),
		["Thirds"] = new(TokenKind.PluralDenominator, 3),
		["Quarter"] = new(TokenKind.SingularDenominator, 4),
		["Quarters"] = new(TokenKind.PluralDenominator, 4),
		["Fourth"] = new(TokenKind.SingularDenominator, 4),
		["Fourths"] = new(TokenKind.PluralDenominator, 4),
		["Fifth"] = new(TokenKind.SingularDenominator, 5),
		["Fifths"] = new(TokenKind.PluralDenominator, 5),
		["Sixth"] = new(TokenKind.SingularDenominator, 6),
		["Sixths"] = new(TokenKind.PluralDenominator, 6),
		["Seventh"] = new(TokenKind.SingularDenominator, 7),
		["Sevenths"] = new(TokenKind.PluralDenominator, 7),
		["Eighth"] = new(TokenKind.SingularDenominator, 8),
		["Eighths"] = new(TokenKind.PluralDenominator, 8),
		["Ninth"] = new(TokenKind.SingularDenominator, 9),
		["Ninths"] = new(TokenKind.PluralDenominator, 9),
		["Tenth"] = new(TokenKind.SingularDenominator, 10),
		["Tenths"] = new(TokenKind.PluralDenominator, 10),
		["Eleventh"] = new(TokenKind.SingularDenominator, 11),
		["Elevenths"] = new(TokenKind.PluralDenominator, 11),
		["Twelfth"] = new(TokenKind.SingularDenominator, 12),
		["Twelfths"] = new(TokenKind.PluralDenominator, 12),
		["Twentieth"] = new(TokenKind.SingularDenominator, 20),
		["Twentieths"] = new(TokenKind.PluralDenominator, 20),
		["Hundredth"] = new(TokenKind.SingularDenominator, 100),
		["Hundredths"] = new(TokenKind.PluralDenominator, 100),
		["Thousandth"] = new(TokenKind.SingularDenominator, 1000),
		["Thousandths"] = new(TokenKind.PluralDenominator, 1000),
		["Millionth"] = new(TokenKind.SingularDenominator, 1000000),
		["Millionths"] = new(TokenKind.PluralDenominator, 1000000),
	};

	public static bool TryParse(string? text, out Rational result)
	{
		result = default;
		if (!TryTokenize(text, out List<Token> tokens))
		{
			return false;
		}

		int over = tokens.FindIndex(token => token.Kind == TokenKind.Over);
		if (over >= 0)
		{
			if (!TryParseInteger(tokens.GetRange(0, over), out BigInteger numerator)
				|| !TryParseInteger(tokens.GetRange(over + 1, tokens.Count - over - 1), out BigInteger denominator)
				|| denominator.IsZero)
			{
				return false;
			}

			result = new(numerator, denominator);
			return true;
		}

		Token lastToken = tokens[tokens.Count - 1];
		if (lastToken.Kind is TokenKind.SingularDenominator or TokenKind.PluralDenominator)
		{
			tokens.RemoveAt(tokens.Count - 1);
			bool plural = lastToken.Kind == TokenKind.PluralDenominator;
			BigInteger numerator = tokens.Count == 1 && tokens[0].Kind == TokenKind.Minus ? -BigInteger.One : BigInteger.One;
			bool implicitOne = tokens.Count == 0 || (tokens.Count == 1 && tokens[0].Kind == TokenKind.Minus);
			if ((!implicitOne && !TryParseInteger(tokens, out numerator))
				|| plural == BigInteger.Abs(numerator).IsOne)
			{
				return false;
			}

			result = new(numerator, lastToken.Value);
			return true;
		}

		if (!TryParseInteger(tokens, out BigInteger integer))
		{
			return false;
		}

		result = new(integer);
		return true;
	}

	private static bool TryTokenize(string? text, out List<Token> tokens)
	{
		tokens = new();
		if (text is null || text.Length == 0 || text[0] is < 'A' or > 'Z' || text.Any(character => character is not (>= 'A' and <= 'Z' or >= 'a' and <= 'z')))
		{
			return false;
		}

		int start = 0;
		for (int index = 1; index <= text.Length; index++)
		{
			if (index == text.Length || char.IsUpper(text[index]))
			{
				if (!_vocabulary.TryGetValue(text.Substring(start, index - start), out Token token))
				{
					return false;
				}

				tokens.Add(token);
				start = index;
			}
		}

		return true;
	}

	private static bool TryParseInteger(List<Token> tokens, out BigInteger result)
	{
		result = default;
		bool negative = tokens.Count > 0 && tokens[0].Kind == TokenKind.Minus;
		int start = negative ? 1 : 0;
		if (start == tokens.Count)
		{
			return false;
		}

		if (tokens.Count == start + 1 && tokens[start] is { Kind: TokenKind.Number, Value.IsZero: true })
		{
			return true;
		}

		BigInteger total = 0;
		BigInteger previousScale = 0;
		BigInteger group = 0;
		BigInteger previousNumber = 0;
		bool finalRemainder = false;
		for (int index = start; index < tokens.Count; index++)
		{
			Token token = tokens[index];
			switch (token.Kind)
			{
				case TokenKind.Number when token.Value > 0:
					// Only a tens word can be followed directly by another number word.
					if (previousNumber != 0 && !(previousNumber >= 20 && previousNumber % 10 == 0 && token.Value < 10))
					{
						return false;
					}

					group += token.Value;
					previousNumber = token.Value;
					break;

				case TokenKind.Hundred:
					if (group < 1 || group > 9 || finalRemainder)
					{
						return false;
					}

					group *= token.Value;
					previousNumber = 0;
					break;

				case TokenKind.Scale:
					if (group == 0 || (!previousScale.IsZero && token.Value >= previousScale) || finalRemainder)
					{
						return false;
					}

					total += group * token.Value;
					group = 0;
					previousNumber = 0;
					previousScale = token.Value;
					break;

				case TokenKind.And when index > start && index + 1 < tokens.Count && tokens[index + 1].Kind == TokenKind.Number && tokens[index + 1].Value > 0:
					TokenKind previousKind = tokens[index - 1].Kind;
					if (previousKind is not (TokenKind.Hundred or TokenKind.Scale))
					{
						return false;
					}

					// After a large scale, "And" introduces the final part below one hundred.
					finalRemainder = previousKind == TokenKind.Scale;
					break;

				case TokenKind.Minus:
				case TokenKind.Over:
				case TokenKind.SingularDenominator:
				case TokenKind.PluralDenominator:
				default:
					return false;
			}
		}

		result = negative ? -(total + group) : total + group;
		return true;
	}

	private readonly record struct Token(TokenKind Kind, BigInteger Value);

	private enum TokenKind
	{
		Number,
		Hundred,
		Scale,
		Minus,
		And,
		Over,
		SingularDenominator,
		PluralDenominator
	}
}
