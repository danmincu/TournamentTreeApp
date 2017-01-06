//interfaces defined in are in jquery.bracket.john.ts
(function ($) {
    // http://stackoverflow.com/questions/18082/validate-numbers-in-javascript-isnumeric
    function isNumber(n) {
        return !isNaN(parseFloat(n)) && isFinite(n);
    }
    function depth(a) {
        function df(a, d) {
            if (a instanceof Array) {
                return df(a[0], d + 1);
            }
            return d;
        }
        return df(a, 0);
    }
    function wrap(a, d) {
        if (d > 0) {
            a = wrap([a], d - 1);
        }
        return a;
    }
    function emptyTeam() {
        return { source: null, name: null, id: -1, idx: -1, score: null };
    }
    function teamsInResultOrder(match) {
        if (isNumber(match.a.score) && isNumber(match.b.score)) {
            if (match.a.score > match.b.score) {
                return [match.a, match.b];
            }
            else if (match.a.score < match.b.score) {
                return [match.b, match.a];
            }
        }
        return [];
    }
    function matchWinner(match) {
        return teamsInResultOrder(match)[0] || emptyTeam();
    }
    function matchLoser(match) {
        return teamsInResultOrder(match)[1] || emptyTeam();
    }
    function trackHighlighter(teamIndex, cssClass, container) {
        var elements = container.find('.team[data-teamid=' + teamIndex + ']');
        var addedClass = !cssClass ? 'highlight' : cssClass;
        return {
            highlight: function () {
                elements.each(function () {
                    $(this).addClass(addedClass);
                    if ($(this).hasClass('win')) {
                        $(this).parent().find('.connector').addClass(addedClass);
                    }
                });
            },
            deHighlight: function () {
                elements.each(function () {
                    $(this).removeClass(addedClass);
                    $(this).parent().find('.connector').removeClass(addedClass);
                });
            }
        };
    }
    function postProcess(container, w, f) {
        var source = f || w;
        var winner = source.winner();
        var loser = source.loser();
        if (winner && loser) {
            trackHighlighter(winner.idx, 'highlightWinner', container).highlight();
            trackHighlighter(loser.idx, 'highlightLoser', container).highlight();
        }
        container.find('.team').mouseover(function () {
            var i = $(this).attr('data-teamid');
            var track = trackHighlighter(i, null, container);
            track.highlight();
            $(this).mouseout(function () {
                track.deHighlight();
                $(this).unbind('mouseout');
            });
        });
    }
    function defaultEdit(span, data, done) {
        var input = $('<input type="text">');
        input.val(data);
        span.html(input);
        input.focus();
        input.blur(function () {
            done(input.val());
        });
        input.keydown(function (e) {
            var key = (e.keyCode || e.which);
            if (key === 9 /*tab*/ || key === 13 /*return*/ || key === 27 /*esc*/) {
                e.preventDefault();
                done(input.val(), (key !== 27));
            }
        });
    }
    function defaultRender(container, team, score) {
        container.append(team);
    }
    function winnerBubbles(match) {
        var el = match.el;
        var winner = el.find('.team.win');
        winner.append('<div class="bubble">1st</div>');
        var loser = el.find('.team.lose');
        loser.append('<div class="bubble">2nd</div>');
        return true;
    }
    function winnerBubblesWithThirdPlace(match) {
        var el = match.el;
        var winner = el.find('.team.win');
        winner.append('<div class="bubble">1st</div>');
        var loser = el.find('.team.lose');
        loser.append('<div class="bubble">2nd</div>');
        var third_1 = match.round().prev().match(0).el.find('.team.lose');
        var fourth_1 = match.round().prev().match(1).el.find('.team.lose');
        third_1.css("z-index", 3);
        fourth_1.css("z-index", 3);
        third_1.append('<div class="bubble third" style="z-index: 1">3rd</div>');
        fourth_1.append('<div class="bubble third" style="z-index: 1">3rd</div>');
        return true;
    }
    function consolationBubbles(match) {
        var el = match.el;
        var winner = el.find('.team.win');
        winner.append('<div class="bubble third">3rd</div>');
        var loser = el.find('.team.lose');
        loser.append('<div class="bubble fourth">4th</div>');
        return true;
    }
    var winnerMatchSources = function (teams, m) { return function () { return [
        { source: function () { return ({ name: teams[m][0], idx: (m * 2) }); } },
        { source: function () { return ({ name: teams[m][1], idx: (m * 2 + 1) }); } }
    ]; }; };
    var winnerAlignment = function (match, skipConsolationRound) { return function (tC) {
        tC.css('top', '');
        tC.css('position', 'absolute');
        if (skipConsolationRound) {
            tC.css('top', (match.el.height() / 2 - tC.height() / 2) + 'px');
        }
        else {
            tC.css('bottom', (-tC.height() / 2) + 'px');
        }
    }; };
    function prepareWinners(winners, teams, isSingleElimination, skipConsolationRound, skipGrandFinalComeback) {
        var rounds = Math.log(teams.length * 2) / Math.log(2);
        var matches = teams.length;
        var round;
        for (var r = 0; r < rounds; r += 1) {
            round = winners.addRound();
            for (var m = 0; m < matches; m += 1) {
                var teamCb = (r === 0) ? winnerMatchSources(teams, m) : null;
                if (!(r === rounds - 1 && isSingleElimination) && !(r === rounds - 1 && skipGrandFinalComeback)) {
                    round.addMatch(teamCb);
                }
                else {
                    var match = round.addMatch(teamCb, skipConsolationRound ? winnerBubblesWithThirdPlace : winnerBubbles);
                    if (!skipGrandFinalComeback) {
                        match.setAlignCb(winnerAlignment(match, skipConsolationRound));
                    }
                }
            }
            matches /= 2;
        }
        var roundCon = $('<div class="round"></div>');
        roundCon.appendTo(winners.el);
        if (isSingleElimination) {
            winners.final().connectorCb(function () {
                return null;
            });
            if (teams.length > 1 && !skipConsolationRound) {
                var third_2 = winners.final().round().prev().match(0).loser;
                var fourth_2 = winners.final().round().prev().match(1).loser;
                var consol_1 = round.addMatch(function () {
                    return [
                        { source: third_2 },
                        { source: fourth_2 }
                    ];
                }, consolationBubbles);
                consol_1.setAlignCb(function (tC) {
                    var height = (winners.el.height()) / 2;
                    consol_1.el.css('height', (height) + 'px');
                    var topShift = tC.height();
                    tC.css('top', (topShift) + 'px');
                });
                consol_1.connectorCb(function () {
                    return null;
                });
            }
        }
    }
    var loserMatchSources = function (winners, losers, matches, m, n, r) { return function () {
        /* first round comes from winner bracket */
        if (n % 2 === 0 && r === 0) {
            return [
                { source: winners.round(0).match(m * 2).loser },
                { source: winners.round(0).match(m * 2 + 1).loser }
            ];
        }
        else {
            /* To maximize the time it takes for two teams to play against
             * eachother twice, WB losers are assigned in reverse order
             * every second round of LB */
            var winnerMatch = (r % 2 === 0) ? (matches - m - 1) : m;
            return [
                { source: losers.round(r * 2).match(m).winner },
                { source: winners.round(r + 1).match(winnerMatch).loser }
            ];
        }
    }; };
    var loserAlignment = function (teamCon, match) { return function () { return teamCon.css('top', (match.el.height() / 2 - teamCon.height() / 2) + 'px'); }; };
    function prepareLosers(winners, losers, teamCount, skipGrandFinalComeback) {
        var rounds = Math.log(teamCount * 2) / Math.log(2) - 1;
        var matches = teamCount / 2;
        for (var r = 0; r < rounds; r += 1) {
            /* if player cannot rise back to grand final, last round of loser
             * bracket will be player between two LB players, eliminating match
             * between last WB loser and current LB winner */
            var subRounds = (skipGrandFinalComeback && r === (rounds - 1) ? 1 : 2);
            for (var n = 0; n < subRounds; n += 1) {
                var round = losers.addRound();
                for (var m = 0; m < matches; m += 1) {
                    var teamCb = (!(n % 2 === 0 && r !== 0)) ? loserMatchSources(winners, losers, matches, m, n, r) : null;
                    var isLastMatch = r === rounds - 1 && skipGrandFinalComeback;
                    var match = round.addMatch(teamCb, isLastMatch ? consolationBubbles : null);
                    match.setAlignCb(loserAlignment(match.el.find('.teamContainer'), match));
                    if (isLastMatch) {
                        // Override default connector
                        match.connectorCb(function () {
                            return null;
                        });
                    }
                    else if (r < rounds - 1 || n < 1) {
                        var cb = (n % 2 === 0) ? function (tC, match) {
                            // inside lower bracket
                            var connectorOffset = tC.height() / 4;
                            var height = 0;
                            var shift = 0;
                            if (match.winner().id === 0) {
                                shift = connectorOffset;
                            }
                            else if (match.winner().id === 1) {
                                height = -connectorOffset * 2;
                                shift = connectorOffset;
                            }
                            else {
                                shift = connectorOffset * 2;
                            }
                            return { height: height, shift: shift };
                        } : null;
                        match.connectorCb(cb);
                    }
                }
            }
            matches /= 2;
        }
    }
    function prepareFinals(finals, winners, losers, skipSecondaryFinal, skipConsolationRound, topCon) {
        var round = finals.addRound();
        var match = round.addMatch(function () {
            return [
                { source: winners.winner },
                { source: losers.winner }
            ];
        }, function (match) {
            /* Track if container has been resized for final rematch */
            var _isResized = false;
            /* LB winner won first final match, need a new one */
            if (!skipSecondaryFinal && (match.winner().name !== null && match.winner().name === losers.winner().name)) {
                if (finals.size() === 2) {
                    return;
                }
                /* This callback is ugly, would be nice to make more sensible solution */
                var round_1 = finals.addRound(function () {
                    var rematch = ((match.winner().name !== null && match.winner().name === losers.winner().name));
                    if (_isResized === false) {
                        if (rematch) {
                            _isResized = true;
                            topCon.css('width', (parseInt(topCon.css('width'), 10) + 140) + 'px');
                        }
                    }
                    if (!rematch && _isResized) {
                        _isResized = false;
                        finals.dropRound();
                        topCon.css('width', (parseInt(topCon.css('width'), 10) - 140) + 'px');
                    }
                    return rematch;
                });
                /* keep order the same, WB winner top, LB winner below */
                var match2_1 = round_1.addMatch(function () {
                    return [
                        { source: match.first },
                        { source: match.second }
                    ];
                }, winnerBubbles);
                match.connectorCb(function (tC) {
                    return { height: 0, shift: tC.height() / 2 };
                });
                match2_1.connectorCb(function () {
                    return null;
                });
                match2_1.setAlignCb(function (tC) {
                    var height = (winners.el.height() + losers.el.height());
                    match2_1.el.css('height', (height) + 'px');
                    var topShift = (winners.el.height() / 2 + winners.el.height() + losers.el.height() / 2) / 2 - tC.height();
                    tC.css('top', (topShift) + 'px');
                });
                return false;
            }
            else {
                return winnerBubbles(match);
            }
        });
        match.setAlignCb(function (tC) {
            var height = (winners.el.height() + losers.el.height());
            if (!skipConsolationRound) {
                height /= 2;
            }
            match.el.css('height', (height) + 'px');
            var topShift = (winners.el.height() / 2 + winners.el.height() + losers.el.height() / 2) / 2 - tC.height();
            tC.css('top', (topShift) + 'px');
        });
        if (!skipConsolationRound) {
            var fourth_3 = losers.final().round().prev().match(0).loser;
            var consol_2 = round.addMatch(function () {
                return [
                    { source: fourth_3 },
                    { source: losers.loser }
                ];
            }, consolationBubbles);
            consol_2.setAlignCb(function (tC) {
                var height = (winners.el.height() + losers.el.height()) / 2;
                consol_2.el.css('height', (height) + 'px');
                var topShift = (winners.el.height() / 2 + winners.el.height() + losers.el.height() / 2) / 2 + tC.height() / 2 - height;
                tC.css('top', (topShift) + 'px');
            });
            match.connectorCb(function () {
                return null;
            });
            consol_2.connectorCb(function () {
                return null;
            });
        }
        winners.final().connectorCb(function (tC) {
            var shift;
            var height;
            var connectorOffset = tC.height() / 4;
            var topShift = (winners.el.height() / 2 + winners.el.height() + losers.el.height() / 2) / 2 - tC.height() / 2;
            var matchupOffset = topShift - winners.el.height() / 2;
            if (winners.winner().id === 0) {
                height = matchupOffset + connectorOffset * 2;
                shift = connectorOffset;
            }
            else if (winners.winner().id === 1) {
                height = matchupOffset;
                shift = connectorOffset * 3;
            }
            else {
                height = matchupOffset + connectorOffset;
                shift = connectorOffset * 2;
            }
            height -= tC.height() / 2;
            return { height: height, shift: shift };
        });
        losers.final().connectorCb(function (tC) {
            var shift;
            var height;
            var connectorOffset = tC.height() / 4;
            var topShift = (winners.el.height() / 2 + winners.el.height() + losers.el.height() / 2) / 2 - tC.height() / 2;
            var matchupOffset = topShift - winners.el.height() / 2;
            if (losers.winner().id === 0) {
                height = matchupOffset;
                shift = connectorOffset * 3;
            }
            else if (losers.winner().id === 1) {
                height = matchupOffset + connectorOffset * 2;
                shift = connectorOffset;
            }
            else {
                height = matchupOffset + connectorOffset;
                shift = connectorOffset * 2;
            }
            height += tC.height() / 2;
            return { height: -height, shift: -shift };
        });
    }
    function mkRound(bracket, previousRound, roundIdx, results, doRenderCb, mkMatch) {
        var matches = [];
        var roundCon = $('<div class="round"></div>');
        return {
            el: roundCon,
            bracket: bracket,
            id: roundIdx,
            addMatch: function (teamCb, renderCb) {
                var matchIdx = matches.length;
                var teams = (teamCb !== null) ? teamCb() : [
                    { source: bracket.round(roundIdx - 1).match(matchIdx * 2).winner },
                    { source: bracket.round(roundIdx - 1).match(matchIdx * 2 + 1).winner }
                ];
                var match = mkMatch(this, teams, matchIdx, !results ? null : results[matchIdx], renderCb);
                matches.push(match);
                return match;
            },
            match: function (id) {
                return matches[id];
            },
            prev: function () {
                return previousRound;
            },
            size: function () {
                return matches.length;
            },
            render: function () {
                roundCon.empty();
                if (typeof (doRenderCb) === 'function' && !doRenderCb()) {
                    return;
                }
                roundCon.appendTo(bracket.el);
                $.each(matches, function (i, ma) {
                    ma.render();
                });
            },
            results: function () {
                var results = [];
                $.each(matches, function (i, ma) {
                    results.push(ma.results());
                });
                return results;
            }
        };
    }
    function mkBracket(bracketCon, results, mkMatch) {
        var rounds = [];
        return {
            el: bracketCon,
            addRound: function (doRenderCb) {
                var id = rounds.length;
                var previous = (id > 0) ? rounds[id - 1] : null;
                var round = mkRound(this, previous, id, !results ? null : results[id], doRenderCb, mkMatch);
                rounds.push(round);
                return round;
            },
            dropRound: function () {
                rounds.pop();
            },
            round: function (id) {
                return rounds[id];
            },
            size: function () {
                return rounds.length;
            },
            final: function () {
                return rounds[rounds.length - 1].match(0);
            },
            winner: function () {
                return rounds[rounds.length - 1].match(0).winner();
            },
            loser: function () {
                return rounds[rounds.length - 1].match(0).loser();
            },
            render: function () {
                bracketCon.empty();
                /* Length of 'rounds' can increase during render in special case when
                 LB win in finals adds new final round in match render callback.
                 Therefore length must be read on each iteration. */
                for (var i = 0; i < rounds.length; i += 1) {
                    rounds[i].render();
                }
            },
            results: function () {
                var results = [];
                $.each(rounds, function (i, ro) {
                    results.push(ro.results());
                });
                return results;
            }
        };
    }
    function connector(height, shift, teamCon, align) {
        var width = parseInt($('.round:first').css('margin-right'), 10) / 2;
        var drop = true;
        // drop:
        // [team]'\
        //         \_[team]
        // !drop:
        //         /'[team]
        // [team]_/
        if (height < 0) {
            drop = false;
            height = -height;
        }
        /* straight lines are prettier */
        if (height < 2) {
            height = 0;
        }
        var src = $('<div class="connector"></div>').appendTo(teamCon);
        src.css('height', height);
        src.css('width', width + 'px');
        src.css(align, (-width - 2) + 'px');
        if (shift >= 0) {
            src.css('top', shift + 'px');
        }
        else {
            src.css('bottom', (-shift) + 'px');
        }
        if (drop) {
            src.css('border-bottom', 'none');
        }
        else {
            src.css('border-top', 'none');
        }
        var dst = $('<div class="connector"></div>').appendTo(src);
        dst.css('width', width + 'px');
        dst.css(align, -width + 'px');
        if (drop) {
            dst.css('bottom', '0px');
        }
        else {
            dst.css('top', '0px');
        }
        return src;
    }
    var JqueryBracket = function (opts) {
        var align = opts.dir === 'lr' ? 'right' : 'left';
        var resultIdentifier;
        if (!opts) {
            throw Error('Options not set');
        }
        if (!opts.el) {
            throw Error('Invalid jQuery object as container');
        }
        if (!opts.init && !opts.save) {
            throw Error('No bracket data or save callback given');
        }
        if (opts.userData === undefined) {
            opts.userData = null;
        }
        if (opts.decorator && (!opts.decorator.edit || !opts.decorator.render)) {
            throw Error('Invalid decorator input');
        }
        else if (!opts.decorator) {
            opts.decorator = { edit: defaultEdit, render: defaultRender };
        }
        var data;
        if (!opts.init) {
            opts.init = {
                teams: [
                    ['', '']
                ],
                results: [],
                divisionId: ''
            };
        }
        data = opts.init;
        var topCon = $('<div class="jQBracket ' + opts.dir + '"></div>').appendTo(opts.el.empty());
        var w, l, f;
        function renderAll(save) {
            resultIdentifier = 0;
            w.render();
            if (l) {
                l.render();
            }
            if (f && !opts.skipGrandFinalComeback) {
                f.render();
            }
            postProcess(topCon, w, f);
            if (save) {
                data.results[0] = w.results();
                if (l) {
                    data.results[1] = l.results();
                }
                if (f && !opts.skipGrandFinalComeback) {
                    data.results[2] = f.results();
                }
                if (opts.save) {
                    opts.save(data, opts.userData);
                }
            }
        }
        function mkMatch(round, data, idx, results, renderCb) {
            var match = { a: data[0], b: data[1] };
            function teamElement(round, team, isReady) {
                var rId = resultIdentifier;
                var sEl = $('<div class="score" data-resultid="result-' + rId + '"></div>');
                //initialize all the "ready" teams with a score of 0!
                if (team.name && isReady && !isNumber(team.score))
                    team.score = 0;
                var score = (!team.name || !isReady)
                    ? '  '
                    : (!isNumber(team.score) ? '  ' : team.score);
                //sEl.append(score);
                if (isNumber(score) && score > 0) {
                    // sEl.append("<img src='http://www.clker.com/cliparts/e/2/a/d/1206574733930851359Ryan_Taylor_Green_Tick.svg.med.png' style='width:10px;height: 10px;'/>");
                    //const checkm = $('<span class="checkmark"><div class="checkmark_circle"></div>< div class="checkmark_stem" > </div>< div class="checkmark_kick" > </div>< /span>');
                    var checkm = $('<span class="checkmark"><div class="checkmark_circle"></div><div class="checkmark_stem"></div><div class="checkmark_kick"></div></span>');
                    sEl.append(checkm);
                }
                else {
                    var checkmUnset = $('<span class="checkmark"><div class="checkmark_circle"></div></div></span>');
                    sEl.append(checkmUnset);
                }
                resultIdentifier += 1;
                var name = !team.name ? '  ' : team.name;
                var tEl = $('<div class="team"></div>');
                var nEl = $('<div class="label"></div>').appendTo(tEl);
                //const nSc = $('<div class="schoolLabel"></div>').appendTo(tEl);
                if (round === 0) {
                    tEl.attr('data-resultid', 'team-' + rId);
                }
                if (name.indexOf("|") > 0) {
                    var ntEl = $('<div>' + name.split("|")[0] + '</div><div class="schoolLabel">' + name.split("|")[1] + '</div>');
                    nEl.append(ntEl);
                }
                else
                    opts.decorator.render(nEl, name, score);
                //nSc.append("school123");
                // opts.decorator.render(nSc, "school123", score);
                if (isNumber(team.idx)) {
                    tEl.attr('data-teamid', team.idx);
                }
                if (team.name === null) {
                    tEl.addClass('na');
                }
                else if (matchWinner(match).name === team.name) {
                    tEl.addClass('win');
                }
                else if (matchLoser(match).name === team.name) {
                    tEl.addClass('lose');
                }
                tEl.append(sEl);
                if (!(team.name === null || !opts.save) && opts.save) {
                    tEl.click(function () {
                        var span = $(this);
                        var currentTeam = span.attr("data-teamid");
                        var elements = opts.el.find('.team[data-teamid=' + currentTeam + ']');
                        var hasScore = false;
                        if (elements)
                            elements.each(function () {
                                if ($(this).hasClass('win') && !$(this).parent().parent().hasClass('hiddenMatch')) {
                                    hasScore = true;
                                }
                                ;
                                if ($(this).hasClass('lose')) {
                                    hasScore = true;
                                }
                            });
                        if (hasScore)
                            return;
                        //leave the function as the team is scored already
                        if (span.attr("clicked") == "0") {
                            dragFct(span);
                        }
                        else {
                            span.attr("clicked", "0");
                            setTimeout(function () {
                                span.attr("clicked", null);
                            }, 2000);
                        }
                        //function unclick() {
                        //    const clickedNodes = opts.el.find('.team[clicked=0]');
                        //    if (clickedNodes.length > 0) {
                        //        clickedNodes.attr("clicked", null);
                        //        console.log("removing click");
                        //    }
                        //}
                        //setTimeout(unclick, 2000);
                    });
                    //tEl.dblclick(
                    var dragFct = function (span) {
                        //const span = $(this);
                        var otherDraggedNodes = opts.el.find('.team[switched=0]');
                        if (otherDraggedNodes.length) {
                            span.attr("switched", "0");
                            span.addClass("dragged");
                            otherDraggedNodes.attr("switched", null);
                            span.attr("switched", null);
                            var indexT1 = parseInt(span.attr("data-teamid"));
                            var indexT2 = parseInt(otherDraggedNodes.attr("data-teamid"));
                            var m1 = opts.init.teams[Math.floor(indexT1 / 2)];
                            var m2 = opts.init.teams[Math.floor(indexT2 / 2)];
                            var t1 = m1[indexT1 % 2];
                            var t2 = m2[indexT2 % 2];
                            opts.init.teams[Math.floor(indexT1 / 2)][indexT1 % 2] = t2;
                            opts.init.teams[Math.floor(indexT2 / 2)][indexT2 % 2] = t1;
                            function redrawSwitchedTeams() {
                                JqueryBracket(opts);
                            }
                            setTimeout(redrawSwitchedTeams, 400);
                            if (opts.swap) {
                                opts.swap(t1, t2, opts.init.divisionId);
                            }
                        }
                        else {
                            span.attr("switched", "0");
                            span.addClass("dragged");
                        }
                    };
                    //);
                    //nEl.addClass('editable');
                    //nEl.click(function () {
                    //    const span = $(this);
                    //    function editor() {
                    //        function done_fn(val, next: boolean) {
                    //            if (val) {
                    //                opts.init.teams[~~(team.idx / 2)][team.idx % 2] = val;
                    //            }
                    //            renderAll(true);
                    //            span.click(editor);
                    //            const labels = opts.el.find('.team[data-teamid=' + (team.idx + 1) + '] div.label:first');
                    //            if (labels.length && next === true && round === 0) {
                    //                $(labels).click();
                    //            }
                    //        }
                    //        span.unbind();
                    //        opts.decorator.edit(span, team.name, done_fn);
                    //    }
                    //    editor();
                    //});
                    if (team.name && isReady) {
                        sEl.addClass('editable');
                        sEl.click(function () {
                            //new simplified rule that switches value when clicked
                            if (!isNumber(team.score) || team.score > 0)
                                team.score = 0;
                            else
                                team.score = 1;
                            renderAll(true);
                            //const span = $(this);
                            //function editor() {
                            //    span.unbind();
                            //    const score = !isNumber(team.score) ? '0' : span.text();
                            //    const input = $('<input type="text">');                          
                            //    input.val(score);
                            //    span.html(input);
                            //    input.focus().select();
                            //    input.keydown(function (e) {
                            //        if (!isNumber($(this).val())) {
                            //            $(this).addClass('error');
                            //        }
                            //        else {
                            //            $(this).removeClass('error');
                            //        }
                            //        const key = (e.keyCode || e.which);
                            //        if (key === 9 || key === 13 || key === 27) {
                            //            e.preventDefault();
                            //            $(this).blur();
                            //            if (key === 27) {
                            //                return;
                            //            }
                            //            const next = topCon.find('div.score[data-resultid=result-' + (rId + 1) + ']');
                            //            if (next) {
                            //                next.click();
                            //            }
                            //        }
                            //    });
                            //    input.blur(function () {
                            //        var val = input.val();
                            //        if ((!val || !isNumber(val)) && !isNumber(team.score)) {
                            //            val = '0';
                            //        }
                            //        else if ((!val || !isNumber(val)) && isNumber(team.score)) {
                            //            val = team.score;
                            //        }
                            //        span.html(val);
                            //        if (isNumber(val)) {
                            //            team.score = parseInt(val, 10);
                            //            renderAll(true);
                            //        }
                            //        span.click(editor);
                            //    });
                            //}
                            //editor();
                        });
                    }
                }
                return tEl;
            }
            var connectorCb = null;
            var alignCb = null;
            var matchCon = $('<div class="match"></div>');
            var teamCon = $('<div class="teamContainer"></div>');
            if (!opts.save) {
                var matchUserData_1 = (results ? results[2] : null);
                if (opts.onMatchHover) {
                    teamCon.hover(function () {
                        opts.onMatchHover(matchUserData_1, true);
                    }, function () {
                        opts.onMatchHover(matchUserData_1, false);
                    });
                }
                if (opts.onMatchClick) {
                    teamCon.click(function () { opts.onMatchClick(matchUserData_1); });
                }
            }
            match.a.id = 0;
            match.b.id = 1;
            match.a.name = match.a.source().name;
            match.b.name = match.b.source().name;
            match.a.score = !results ? null : results[0];
            match.b.score = !results ? null : results[1];
            /* match has score even though teams haven't yet been decided */
            /* todo: would be nice to have in preload check, maybe too much work */
            if ((!match.a.name || !match.b.name) && (isNumber(match.a.score) || isNumber(match.b.score))) {
                console.log('ERROR IN SCORE DATA: ' + match.a.source().name + ': ' +
                    match.a.score + ', ' + match.b.source().name + ': ' + match.b.score);
                match.a.score = match.b.score = null;
            }
            return {
                el: matchCon,
                id: idx,
                round: function () {
                    return round;
                },
                connectorCb: function (cb) {
                    connectorCb = cb;
                },
                connect: function (cb) {
                    var connectorOffset = teamCon.height() / 4;
                    var matchupOffset = matchCon.height() / 2;
                    var shift;
                    var height;
                    if (!cb || cb === null) {
                        if (idx % 2 === 0) {
                            if (this.winner().id === 0) {
                                shift = connectorOffset;
                                height = matchupOffset;
                            }
                            else if (this.winner().id === 1) {
                                shift = connectorOffset * 3;
                                height = matchupOffset - connectorOffset * 2;
                            }
                            else {
                                shift = connectorOffset * 2;
                                height = matchupOffset - connectorOffset;
                            }
                        }
                        else {
                            if (this.winner().id === 0) {
                                shift = -connectorOffset * 3;
                                height = -matchupOffset + connectorOffset * 2;
                            }
                            else if (this.winner().id === 1) {
                                shift = -connectorOffset;
                                height = -matchupOffset;
                            }
                            else {
                                shift = -connectorOffset * 2;
                                height = -matchupOffset + connectorOffset;
                            }
                        }
                    }
                    else {
                        var info = cb(teamCon, this);
                        if (info === null) {
                            return;
                        }
                        shift = info.shift;
                        height = info.height;
                    }
                    teamCon.append(connector(height, shift, teamCon, align));
                },
                winner: function () { return matchWinner(match); },
                loser: function () { return matchLoser(match); },
                first: function () {
                    return match.a;
                },
                second: function () {
                    return match.b;
                },
                setAlignCb: function (cb) {
                    alignCb = cb;
                },
                render: function () {
                    matchCon.empty();
                    teamCon.empty();
                    match.a.name = match.a.source().name;
                    match.b.name = match.b.source().name;
                    // hide the match box when there is a BYE ===> on the second name
                    if (match.b.name != null && match.b.name.lastIndexOf('===>') > 0)
                        matchCon.addClass("hiddenMatch");
                    match.a.idx = match.a.source().idx;
                    match.b.idx = match.b.source().idx;
                    if (!matchWinner(match).name) {
                        teamCon.addClass('np');
                    }
                    else {
                        teamCon.removeClass('np');
                    }
                    // Coerce truthy/falsy "isset()" for Typescript
                    var isReady = ((Boolean(match.a.name) || match.a.name === '')
                        && (Boolean(match.b.name) || match.b.name === ''));
                    teamCon.append(teamElement(round.id, match.a, isReady));
                    teamCon.append(teamElement(round.id, match.b, isReady));
                    matchCon.appendTo(round.el);
                    matchCon.append(teamCon);
                    this.el.css('height', (round.bracket.el.height() / round.size()) + 'px');
                    teamCon.css('top', (this.el.height() / 2 - teamCon.height() / 2) + 'px');
                    /* todo: move to class */
                    if (alignCb !== null) {
                        alignCb(teamCon);
                    }
                    var isLast = (typeof (renderCb) === 'function') ? renderCb(this) : false;
                    if (!isLast) {
                        this.connect(connectorCb);
                    }
                },
                results: function () {
                    return [match.a.score, match.b.score];
                }
            };
        }
        /* wrap data to into necessary arrays */
        var r = wrap(data.results, 4 - depth(data.results));
        data.results = r;
        var isSingleElimination = (r.length <= 1);
        if (opts.skipSecondaryFinal && isSingleElimination) {
            $.error('skipSecondaryFinal setting is viable only in double elimination mode');
        }
        if (opts.save && !opts.hideTools) {
            embedEditButtons(topCon, data, opts);
        }
        var fEl, wEl, lEl;
        if (isSingleElimination) {
            wEl = $('<div class="bracket"></div>').appendTo(topCon);
        }
        else {
            if (!opts.skipGrandFinalComeback) {
                fEl = $('<div class="finals"></div>').appendTo(topCon);
            }
            wEl = $('<div class="bracket"></div>').appendTo(topCon);
            lEl = $('<div class="loserBracket"></div>').appendTo(topCon);
        }
        //new
        //const teamContainerHeight = 94;
        var teamContainerHeight = 155;
        var height = data.teams.length * teamContainerHeight;
        wEl.css('height', height);
        // reserve space for consolation round
        if (isSingleElimination && data.teams.length <= 2 && !opts.skipConsolationRound) {
            topCon.css('height', height + 40);
        }
        if (lEl) {
            lEl.css('height', wEl.height() / 2);
        }
        function roundCount(teamCount) {
            if (isSingleElimination) {
                return Math.log(teamCount * 2) / Math.log(2);
            }
            else if (opts.skipGrandFinalComeback) {
                return Math.max(2, (Math.log(teamCount * 2) / Math.log(2) - 1) * 2 - 1); // DE - grand finals
            }
            else {
                return (Math.log(teamCount * 2) / Math.log(2) - 1) * 2 + 1; // DE + grand finals
            }
        }
        var rounds = roundCount(data.teams.length);
        //new - adjust this for overall team width 
        if (opts.save) {
            topCon.css('width', rounds * 270 + 40);
        }
        else {
            topCon.css('width', rounds * 270 + 10);
        }
        w = mkBracket(wEl, !r || !r[0] ? null : r[0], mkMatch);
        if (!isSingleElimination) {
            l = mkBracket(lEl, !r || !r[1] ? null : r[1], mkMatch);
            if (!opts.skipGrandFinalComeback) {
                f = mkBracket(fEl, !r || !r[2] ? null : r[2], mkMatch);
            }
        }
        prepareWinners(w, data.teams, isSingleElimination, opts.skipConsolationRound, opts.skipGrandFinalComeback && !isSingleElimination);
        if (!isSingleElimination) {
            prepareLosers(w, l, data.teams.length, opts.skipGrandFinalComeback);
            if (!opts.skipGrandFinalComeback) {
                prepareFinals(f, w, l, opts.skipSecondaryFinal, opts.skipConsolationRound, topCon);
            }
        }
        renderAll(false);
        return {
            data: function () {
                return opts.init;
            }
        };
    };
    function embedEditButtons(topCon, data, opts) {
        var tools = $('<div class="tools"></div>').appendTo(topCon);
        var inc = $('<span class="increment">+</span>').appendTo(tools);
        inc.click(function () {
            var len = data.teams.length;
            for (var i = 0; i < len; i += 1) {
                data.teams.push(['', '']);
            }
            return JqueryBracket(opts);
        });
        if (data.teams.length > 1 && data.results.length === 1 ||
            data.teams.length > 2 && data.results.length === 3) {
            var dec = $('<span class="decrement">-</span>').appendTo(tools);
            dec.click(function () {
                if (data.teams.length > 1) {
                    data.teams = data.teams.slice(0, data.teams.length / 2);
                    return JqueryBracket(opts);
                }
            });
        }
        if (data.results.length === 1 && data.teams.length > 1) {
            var type = $('<span class="doubleElimination">de</span>').appendTo(tools);
            type.click(function () {
                if (data.teams.length > 1 && data.results.length < 3) {
                    data.results.push([], []);
                    return JqueryBracket(opts);
                }
            });
        }
        else if (data.results.length === 3 && data.teams.length > 1) {
            var type = $('<span class="singleElimination">se</span>').appendTo(tools);
            type.click(function () {
                if (data.results.length === 3) {
                    data.results = data.results.slice(0, 1);
                    return JqueryBracket(opts);
                }
            });
        }
    }
    var methods = {
        init: function (originalOpts) {
            var opts = $.extend(true, {}, originalOpts); // Do not mutate inputs
            var that = this;
            opts.el = this;
            if (opts.save && (opts.onMatchClick || opts.onMatchHover)) {
                $.error('Match callbacks may not be passed in edit mode (in conjunction with save callback)');
            }
            opts.dir = opts.dir || 'lr';
            opts.init.teams = !opts.init.teams || opts.init.teams.length === 0 ? [['', '']] : opts.init.teams;
            opts.skipConsolationRound = opts.skipConsolationRound || false;
            opts.hideTools = opts.hideTools || false;
            opts.skipSecondaryFinal = opts.skipSecondaryFinal || false;
            if (opts.dir !== 'lr' && opts.dir !== 'rl') {
                $.error('Direction must be either: "lr" or "rl"');
            }
            var bracket = JqueryBracket(opts);
            $(this).data('bracket', { target: that, obj: bracket });
            return bracket;
        },
        data: function () {
            var bracket = $(this).data('bracket');
            return bracket.obj.data();
        }
    };
    $.fn.bracket = function (method) {
        if (methods[method]) {
            return methods[method].apply(this, Array.prototype.slice.call(arguments, 1));
        }
        else if (typeof method === 'object' || !method) {
            return methods.init.apply(this, arguments);
        }
        else {
            $.error('Method ' + method + ' does not exist on jQuery.bracket');
        }
    };
})(jQuery);
//# sourceMappingURL=jquery.bracket.john.sd.js.map