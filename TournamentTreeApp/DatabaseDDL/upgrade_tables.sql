
ALTER TABLE Division
ADD [RoundRobin] [bit] NOT NULL DEFAULT(0),
	[DoubleElimination] [bit] NOT NULL DEFAULT(0),
	[NoSecondaryFinal] [bit] NOT NULL DEFAULT(0),
	[NoComebackFromLooserBracket] [bit] NOT NULL DEFAULT(0),
	[ConsolidationRound] [bit] NOT NULL DEFAULT(0)

ALTER TABLE Tournament
ADD [RoundRobin] [bit] NOT NULL DEFAULT(0),
	[DoubleElimination] [bit] NOT NULL DEFAULT(0),
	[NoSecondaryFinal] [bit] NOT NULL DEFAULT(0),
	[NoComebackFromLooserBracket] [bit] NOT NULL DEFAULT(0),
	[ConsolidationRound] [bit] NOT NULL DEFAULT(0)