# Touchline Football Manager — Player Manual

This is a Windows development build, not a certified commercial release. You manage one club in an offline football world. Local language-model agents and automatic coaches run the other clubs; the rules engine validates and executes their actions.

## Start and save

Keep the entire game folder, including LocalAI, together. Start build/TouchlineManager.exe. Configure the bundled local model on the Local AI Settings page once. Future model requests check the service and start it when necessary. A custom localhost service must be started separately. No cloud model is required during play.

Choose New Random World or the embedded original world. Before signing, you may rename all 30 clubs and all players. The player-name editor deliberately hides club membership. After signing, only your own club and players may be renamed. Names do not change identity, attributes or statistics.

Choose a philosophy and a club direction: Funds, Squad Depth, Commercial Empire, Historic Giant or Club Culture. The eligible pool is the top ten clubs by that metric, including ties at the cutoff. A weighted draw favours the stronger metric. The selected philosophy is assigned to your club first; the other clubs then receive random philosophies. Your philosophy is permanent. Existing saves keep their assigned club and philosophy.

A new world has 800 players, completely randomly allocated to 30 clubs in three leagues. Roles are goalkeeper 10%, defender 30%, midfielder 30% and forward 30%; foot preference is independently uniform: 0 both, 1 left, 2 right. There is no guaranteed squad balance. Check cover before starting. Target value is a random integer from 1 to 100; attribute rounding can slightly change the final value. Initial affection and loyalty each roll two six-sided dice. Injuries, fatigue and appearances start at zero. Funds range from 1 to 10000, commercial value from 6 to 35 and honours from 1 to 100. Each club owns five distinct tactics plus No Tactic. The first season has no European competition.

Use Save Game for named saves and Load Game to resume. Automatic saves use atomic replacement and a .bak backup. Completed computer matches create checkpoints. Failed requests preserve the last saved checkpoint; they are not silently treated as successful AI decisions. In animated matches, completed rounds are committed. Save files contain RNG state and can resume mid-match. Original MATLAB and Excel files are not overwritten.

## Screens and decisions

Club Overview shows the next fixture, season stage and alerts. Squad and Lineup shows all squads, but editing, training and naming are restricted to your own club. Select a player to train; arrange slots manually or use Auto Lineup. A healthy goalkeeper is preferred in goal. Injured or suspended players cannot play. There are ten starting slots, not eleven.

Philosophy & Tactics lists every effect. Select a row to read its complete description below the list. Buy an unowned tactic for 1000 funds to unlock it permanently; insufficient funds and duplicate purchases are rejected. Apply the tactic and rearrange the lineup if needed. Only one tactic is active at a time. No Tactic is always available.

Fixtures and Tables shows league/group tables and fixtures. Double-click a completed fixture for its lane review and event report. Competition Statistics separates the three leagues and each domestic cup by league, while Europe combines all participating clubs. Equal values share the same rank.

Transfers shows the market, free agents, squads and pending offers. The offer dialog previews funds after the fee, total wages after signing and the remaining wage surplus. This preview is checked again at completion. Finances and Honours shows cash, commercial income, wages and training points. The Progress Log lists actual decisions and results. The language button selects Chinese or English; custom names remain unchanged.

## Attributes, formation and form

Value and seasonal wage equal max(0, red + green + blue + shot/save - black). Red drives attacking duels and incisive passing; green drives the initial possession contest; blue defends against red and contributes to tackles; shot/save determines shooting or goalkeeper saving ability. Black increases errors: match error weighting uses black + 5. Shot/save is an ability value, not a direct fixed goal percentage. The engine converts it with a bounded logarithmic probability.

Position and foot preference affect weights. In central positions, two-footed players receive a 50% boost to the relevant attribute: central forward shooting, central midfield green or central defence blue. Wide positions favour their matching foot, with asymmetric forward red/shooting adjustments. Wrong player type reduces positive abilities by 10%; a non-goalkeeper in goal, or a goalkeeper used outfield, has no effective shot/save ability for that mismatched role. Non-two-footed goalkeepers receive a shot/save reduction. Automatic lineup uses expected form and the same weighting rules.

Match form is an integer drawn once per participating player between affection and 7, inclusive. Auto lineup estimates round((affection + 7)/2). Form multipliers apply to the four positive abilities; black does not change. Form is not permanently written back as affection.

| Form | Multiplier |
| --- | --- |
| 14 / 13 / 12 / 11 | 125% / 120% / 115% / 110% |
| 10 / 9 / 6–8 | 106% / 103% / 100% |
| 5 / 4 / 3 | 97% / 94% / 90% |
| 2 / 1 / 0 | 85% / 80% / 75% |

Home advantage adds 5% to the four positive abilities, not black. Domestic and European finals are neutral. Philosophy, tactic, position, form, home advantage and prior fatigue combine to determine match abilities.

## Match controls, lanes and fatigue

Matches consist of 45 two-minute rounds. Advance ten minutes, advance to halftime or play to the end. You may change tactics and make substitutions between rounds. Each team may replace at most five players; a substituted player cannot return. The substitution window shows starters and the entire bench with eligibility restrictions.

The pitch animates real possession and passing events. The right panel simultaneously shows both starting lineups: name, slot, original value, current weighted red/green/blue/shot-save, prior fatigue and elapsed minutes. Before a player's form is drawn, the displayed ability is an estimate. Temporary one-round corner effects are not included in that persistent display.

Only the initial green possession contest uses the whole team. Subsequent red-versus-blue duels use the possession lane: own left faces opponent right, centre faces centre. A goalkeeper gaining possession counts as centre; the defending goalkeeper's blue is shared equally between all three lanes. The passer must come from the attacking lane; the shooter can come from any outfield slot. A miss flies outside the pitch and returns to the centre.

Fatigue is assessed per player from actual minutes in that match: at most 30 minutes adds 0; over 30 through 60 adds 1 percentage point; over 60 adds 3. Both the outgoing player and substitute are measured separately. Zero-minute players receive no appearance. Accumulated fatigue reduces the four positive abilities in later matches that week. Weekly recovery clears fatigue and reduces injury/suspension by one week. Earlier, already-settled matches are not recalculated. Legacy live saves reconstruct minutes from their substitution records; incomplete records produce an explicit warning instead of invented minutes.

After a match, actual participants have a 2.5% random injury chance, lasting two to five weeks. Tactical Foul can cause a four-week suspension. It is applied after the match: the player is not removed immediately as with a real-world red card. Injuries and suspensions share the same unavailable counter. Remaining injuries are cleared during seasonal commercial-income settlement; qualifiers may cause new injuries.

## Match reports and progression

Lane reviews count possession wins, attacking duels, successful duels, shots and shots on target by attacking lane, including rebounds. They also show corner and tactical-foul triggers. A shot can be taken by a player from another lane. Older fixtures without these counters are labelled as lacking data. Full reports retain injuries, fatigue changes, training rewards and penalties.

Outside qualifiers, a win earns one training point. A loss consumes one; if none remain, an involved player's attribute is reduced. A goal margin of at least three can reward two winners and penalise two losers. These extra effects are cancelled if the compared lineups differ in value by at least 150 or same-league rank differs by at least five; cross-league matches use only value. Normal victory/defeat point changes still apply. Training has diminishing returns; black training reduces errors.

## Philosophies

The following effects are the implemented rules. Opponent bonuses and penalties are additive percentage-point adjustments to the philosophy's baseline before other factors are multiplied. Each club has one permanent philosophy.

| Philosophy | Effect |
| --- | --- |
| Classic Football | No permanent bonus. Against Nine Forwards, Possession Football or Catenaccio: original forwards blue +15%, midfielders green +15%, defenders red +15%. |
| Total Football | Green +5%. Against Classic Football: team red/blue +10%. Against the seven situational philosophies below: red/blue -3%. No red/blue change against Nine Forwards, Possession Football or Catenaccio. |
| Nine Forwards | Team red +30%, blue -20%. |
| Possession Football | Team green +10%. |
| Catenaccio | Team blue +30%, red -20%. |
| Short Passing | Green +5%. Extra green +15% against Wide and Central Play or Quick Penetration; extra green -15% against Low Block or Counter Attack. |
| Low Block | Blue +5%. Extra blue +15% against Short Passing or Wide and Central Play; extra blue -15% against High Press or Quick Penetration. |
| Quick Penetration | Red +5%. Extra red +15% against Low Block or Long Balls; extra red -15% against Short Passing or High Press. |
| High Press | Red +5%. Blue +15% against Quick Penetration or Low Block; blue -15% against Counter Attack or Long Balls. |
| Counter Attack | Blue +5%. Red +15% against High Press or Short Passing; red -15% against Long Balls or Wide and Central Play. |
| Long Balls | Red +5%. Extra red +15% against Counter Attack or High Press; extra red -15% against Wide and Central Play or Quick Penetration. |
| Wide and Central Play | Green +5%. Extra green +15% against Long Balls or Counter Attack; extra green -15% against Short Passing or Low Block. |

## Tactics

Only one tactic is active. Slot aliases use fwd for forwards, mid for midfielders, def for defenders and gkp for goalkeeper. A remapped slot is reweighted as its new position; it does not add an extra player. Tactics are team-wide only where explicitly stated.

| Tactic | Effect |
| --- | --- |
| No Tactic | No additional effect. |
| Corner Routine | After the opponent makes an initial kickoff/control error: next round, team red +15% and the best shooter shot attribute +15%. |
| Stepping Centre-Back | Reweight def_mid as mid_B2B; that slot gains blue +10%. |
| Shadow Striker | Reweight mid_B2B as fwd_mid; that slot gains green +5%. |
| Double Pivot | Reweight mid_B2B as def_mid; that slot gains green +5%. |
| Star Focus | Highest-value starter: four positive attributes +8%; everyone else -0.5%. |
| Offside Trap | Players whose original type is Defender: red -10%, blue +10%. |
| Park the Bus | Team red -20%, green -20%, blue +50%. |
| Free Roaming Midfielder | Treat mid_B2B as two-footed for weighting; central green bonus +50%. |
| Single Pivot | Treat def_mid as two-footed for weighting; central blue bonus +50%. |
| Central Finisher | Treat fwd_mid as two-footed for weighting; central shot bonus +50%. |
| Attack the Second Ball | fwd_mid shot attribute +5%. |
| Half-Space Passing | mid_left and mid_right green +5%. |
| Cutbacks | fwd_left and fwd_right red +5%. |
| Covering Defence | def_left and def_right blue +5%. |
| Sweeper Keeper | gkp blue +5%. |
| Diagonal Crosses | mid_left and mid_right red +5%. |
| Three Centre-Backs | Reweight def_left and def_right as def_mid. |
| Inverted Midfielders | Reweight mid_left and mid_right as mid_B2B. |
| Cut Inside | fwd_left and fwd_right shot attribute +2.5%. |
| Long Shots | mid_B2B shot attribute +5%. |
| Inverted Full-Backs | Right-footed left-back or left-footed right-back: remove the 0.75 wrong-side blue factor. Type mismatch still applies. |
| Attack Down the Left | Left forward red, left midfielder green, left defender blue +5%; corresponding right-side attributes -5%. |
| Attack Down the Right | Right forward red, right midfielder green, right defender blue +5%; corresponding left-side attributes -5%. |
| Tactical Foul | Cancel the first shot on target before the keeper check, including its rebound. A defender receives a 4-week post-match suspension: 75% corresponding lane, 25% random among three defenders. Disabled in finals; no immediate dismissal. |

Corner Routine receives only an estimated red +1.5% and first-shooter shooting +1.5% when scoring an auto-lineup; actual matches use its triggered effect. Tactical Foul stops one on-target attempt before the goalkeeper decision and any rebound from that attempt. The defender on the matching defensive route bears the suspension with 75% probability; otherwise one of the three defenders is chosen randomly. Finals disable this tactic and treat it as No Tactic.

## Competitions and season order

Each league has ten teams and 18 home/away fixtures per team. The first three weeks contain only league games. Later weeks run league, first-tier cup, second-tier cup, Champions League and Europa League in order. Each domestic league runs its own two cups: two groups of five, double round-robin, top four per group advance. Each European competition has twelve teams in two groups of six, ten group fixtures per team, top four advance. Quarterfinals and semifinals have two legs; finals are single neutral games. Tied aggregate scores are decided by additional simulations until a winner, with a loop guard. Group tables use points, goal difference, goals, head-to-head criteria and saved tie lots.

Each league supplies three direct Champions League places, three qualifying places and two direct Europa League places. League top two and first-tier cup champion qualify directly; league third/fourth and second-tier cup champion enter qualifiers. Duplicates keep the higher place and spare places pass down league order. The nine qualifiers play a double round-robin, sixteen games each; top three join Champions League, the other six join Europa League. Qualifiers count fatigue/injuries but not normal appearances, individual statistics or training-point rewards.

Season-end order: old commercial value ×36 becomes funds; results and statistical awards change commercial value/honours and pay prizes/shares; affection is adjusted; European qualifiers run; Rising Stars age settlement occurs; renewals; player-driven transfers; needs-based transfers; wages; new season. Do not skip unresolved offers or missing legacy appearance data. Commercial value has a floor of five. Wages may make funds negative.

League prize money by places 1–10 is 400, 325, 275, 225, 175, 150, 125, 100, 87.5, 75. Ranking shares are 3%, 3%, 2%, 2%, 2%, 1%, 1%, 1%, 1%, 0% of the league squad-value pool. Every club also gets 200 and a 1% base share. Commercial changes are +3,+2,+1,0,0,0,0,-1,-2,-3; honours are +6,+4,+2 for the top three.

Champions League champion/runner-up/semifinalist/other prizes are 500/250/100/50, ranking shares 8%/6%/4%/2%, plus 1% base share each. Europa prizes are 250/125/50/25 with the same percentages. Champions League commercial/honours changes are +5/+10 for champion, +3/+5 for runner-up; Europa +2/+5 and +1/+2.

First-tier cup champion/runner-up/semifinalist/quarterfinalist prizes are 200/100/37.5/15 and shares 3%/2%/1%/0%. Champion receives +1 commercial and +3 honours, runner-up +1 honour. Second-tier cup prizes are 100/50/20/0 and shares 2%/1%/0%/0%; champion receives +1 honour. Group exits do not receive quarterfinal prizes. Shares use the participating clubs' squad-value pool at settlement.

## Affection and individual awards

Appearance ratio uses actual appearances divided by eligible matches: at least 70% gains one affection; 30% to under 70% is unchanged; 10% to under 30% loses one; under 10% loses two. No eligible matches means no deduction. Affection + loyalty below nine prevents the attendance increase; at least twenty prevents the decrease. Seasonal affection is clamped to 0–12. Qualifiers are excluded from this ledger.

Each of six statistical leaders in each domestic league and in each European competition awards one club honour. No positive statistic or no tournament means no award. Tied leaders favour the best domestic rank, then random selection. Attacking Star uses goals, assists and incisive passes; Playmaking Star uses possession wins; Defensive Star uses tackles and saves. Event weights are 1, 1.5, 0.2, 0.075, 0.16, 1.7; competition weights are league 3, first cup 1, second cup 0.5, Champions League 4, Europa League 2. Each star brings +1 commercial value; one player cannot win multiple star categories. Ties use club achievement weights, then random selection. There is no additional stacked Ballon d'Or commercial award in this build.

## Transfers and budgets

Mark players not to renew during Renewals; they become free agents. Free agents cost no transfer fee. Ordinary base fee is value ×10, multiplied for two-footed players (1.5), value 70–under90 (1.5) or at least90 (2), leaving fewer than three of the seller's outfield type or two goalkeepers (2), affection+loyalty below9 (0.5) or 9–under12 (0.75), desired club (0.75), and value below10 (0.75). Conditions multiply when applicable.

A buyer must afford the fee and all post-transfer wages. The check runs again at completion. Each needs-based window permits five valid active offers, including accepted, rejected or later failed offers; invalid inputs do not count. Player-initiated proposals do not consume these offers. Signing resets affection to two six-sided dice, not loyalty. Accepting a sale does not guarantee player consent. Outfield types with five or fewer players cannot be automatically listed; free agents have no seller to receive a fee.

Low affection+loyalty below12 gives an owned player a leaving tendency; free agents participate too. Clubs are drawn by attraction commercial ×10 + honours ×3, multiplied by1.5 for Champions League qualification or1.2 for Europa qualification. The buyer must consider the player useful in the starting lineup or top two of the relevant role/foot group, and afford wages. The rules engine—not model prose—decides whether a transaction actually completes.

## Career modes and AI

Long-Term Development has no age growth, ageing decline or new rookie generation. Existing older saves default to it. Rising Stars begins with ages18–34. Before renewals, age24 or younger receives nonnegative normal growth with mean10 and standard deviation5, distributed to positive abilities using the generation logic. From31, decline chance is10×(age−30)%, capped at100%. On a decline, increment cumulative decline count; sample the percentage with mean count squared, standard deviation3, clamped0–100%. Positive abilities shrink; black is unchanged. Value is recomputed, so its percentage change need not exactly match ability decline. Increase age by one afterward. Zero-value players retire; there is no forced age cap. Manual retirement is permanent and available in Renewals. Annual rookie count is a rounded normal draw with mean45, standard deviation9 and minimum0; rookies are18-year-old free agents with target values1–50.

Each AI club has a persistent hidden operating personality: profit, sporting ambition, European focus, league focus, loyalty, stars, youth, survival, cups or system building. Observe actions rather than expecting a visible personality label. Rich star-building identities require suitable initial commercial value and funds. Club relationships are symmetric: random1–200, minus100 for same-league clubs. Higher means friendlier; it informs model decisions, not direct match bonuses. No automatic match-result relationship evolution is implemented.

Agents choose a six-week plan at the start of the season and review after weeks6,12,18. They may nominate up to three extra priority fixtures per block. At those sixth-week reviews they allocate available training points or keep them. Automatic coaches no longer spend points; the human manager can train at any time. Semifinals, finals, European quarterfinals and late direct rank-boundary contests are protected important games. Player matches retain local-model decisions. Routine coaches may rotate and substitute according to score, schedule and suitable bench strength. Actual model and automatic-coach actions are logged separately. Three invalid ordinary decisions may fall back conservatively; strict-model matches pause instead. Connection failures pause and are never presented as successful AI decisions.

## Before advancing

Check healthy goalkeeper cover, a legal ten-player lineup, positional depth, fatigue and the remaining week's schedule. Keep wage reserves before buying players or tactics. Growth can increase future wages. Automatic selection is a weighted heuristic and model decisions can be mistaken; neither guarantees optimal results. Read the actual event report. This development build has not completed broad multi-seed economic or competitive-balance certification.

### Agent reliability update
At halftime the model chooses from validated substitution plans (including no changes, single and multiple changes) and chooses its tactic. Candidates exclude injured or previously used substitutes, repeated players and repeated slots, and respect the five-substitution cap. This candidate set does not enumerate every possible combination. Strict player-opponent games still pause on repeated invalid output rather than switching to a fixed coach.
Public decision summaries and new club memories are generated from structured actions. Accepting an offer is a request, not a guarantee of completion; the execution result is shown separately. Raw model explanations are retained only in local diagnostic logs. Historical memories are not rewritten.
Use Local AI settings > Open diagnostic logs. Logs are stored in the local application data TouchlineManager/diagnostics folder, with a 1 MiB current file and one rotated file. They capture startup output, request errors and invalid decisions. Logging failures do not interrupt play.


### Interface and decision protocol update
The match screen has an Opponent decisions panel, latest first. It records actual pre-match tactics, halftime tactic changes, substitutions (players out/in and position), and explicit no-change decisions. Records persist with the fixture. Older saves show the current tactic without inventing missing history. Both squad tables remain available; smaller windows allow scrolling.

At six-week reviews the model chooses a budget-safe training plan, including retaining all points, concentrated or distributed development, and partial spending. Candidates do not enumerate every player and allocation and are not guaranteed optimal. Manual player-manager training is unaffected. Each quote receives a single accept, reject or defer choice. Execution still checks money, wages and player consent. Three-invalid fallback and strict player-opponent pause rules remain in place.

The interface uses navy and teal, gold decision headings and subtle gradients. Sidebar navigation includes hover transitions, an active-page marker and keyboard focus feedback.

### First-team and reserve templates
Automatic selection stores first-team and reserve lineups for each available tactic, then repairs them before a match. Leagues, both European competitions, qualifiers, first-cup knockout games and second-cup semifinals/finals use the first-team template. Other domestic cup matches use reserves. This does not change which games require a local-model coach.

Injuries force replacements and healthy goalkeepers retain priority. Fatigue of at least 6% may justify a suitable, fresher substitute. In reserve matches, players with at least three fewer appearances may receive a chance if their weighted positional ability is at least 85% of the incumbent. Each candidate permits at most two discretionary changes in addition to forced replacements. Temporary absences do not erase templates. Transfers, retirement, abilities, affection, philosophy, tactics and a new season invalidate the cache.

The model still chooses a tactic and lineup candidate; player opponents remain model-controlled. Manually selected player lineups are preserved unless Auto-select is clicked. Cached local computation does not guarantee a noticeable reduction in model inference latency. Duplicate player names are allowed: lineups, transactions and statistics use unique IDs. Club names remain unique.

### Local inference acceleration
The bundled model prefers the included CUDA runtime on compatible NVIDIA systems. Missing or failed GPU initialization falls back to CPU model inference, never to a fixed coach merely because acceleration is unavailable. Backend startup and failures are recorded in diagnostic logs.

Repeated input records are represented by columns and rows without removing data. GPU requests omit free-form model explanations; public summaries and the opponent panel still report executed structured actions. CPU requests retain the explanation protocol because removing it alone did not improve the tested CPU workload.

With the bundled model configured, entering a game starts model loading in the background. Closing the game cancels pending warmup. No club decisions execute in advance. Existing prompt caching remains enabled, reusing computation of identical prefixes rather than old decisions. Actual requests still read current tactics, injuries, fatigue, finances and other state. Cold-start and warmed-up latency differ. Custom local services retain their own lifecycle management.

### Ten-minute coaching and on-field position swaps
Against the player, the local-model opponent decides at 10,20,30,40,50,60,70 and80 minutes, in addition to the existing44-minute halftime checkpoint (the engine uses two-minute rounds). No decision runs at the90-minute finish. It may keep the current setup or select substitutions, one legal outfield position swap and a tactic. The five-substitution limit applies to the whole match. Other AI-vs-AI fixtures do not gain these ten-minute calls.

Completed checkpoints persist in live saves; reloads do not repeat them or backfill elapsed checkpoints. The model sees the score, match clock, actual minutes per current player, remaining substitution quota and eligible bench. Invalid strict-opponent output still pauses after three attempts, and connection errors or cancellation do not count as completed decisions.

Use Swap positions to select two of your ten on-field players. This uses no substitution quota, remains available after all five substitutions, and never restores an off-field player or changes recorded minutes, appearances or fatigue. Positional weights are recalculated and healthy-goalkeeper priority remains. AI swap candidates cover one outfield pair, not every possible lineup permutation. Animations use the lineup that actually played the round before showing the subsequent coaching adjustments.

Tactics and effects previews every owned tactic using the game rule descriptions. Only Apply tactic changes the active tactic; browsing or Cancel does not. Tactical Foul remains disabled in finals. The opponent panel and match report record actual tactic changes, substitutions and position swaps. Extra model calls increase the total waiting time for a player match.

### Improved live decisions for the player opponent

The local model still decides every ten minutes, but now selects one jointly evaluated tactic, substitution and position-swap option. Keeping the current setup is always available. Candidate estimates use public abilities, lane matchups, goalkeeper, home advantage, philosophy, score and time; adjustment costs discourage churn. These are heuristic indicators, not true expected goals or win probabilities.

AI candidates normally allow at most one cumulative substitution before minute 30, two before minute 60 (three when trailing by at least two goals), four before minute 76 and five afterwards. A substitute with fewer than 20 actual minutes is normally protected, except for a major positional correction. Clearly weaker replacements and unnecessary early goalkeeper changes are filtered. These candidate rules do not restrict the player's five substitutions. Corner tactics retain the existing 1.5% selection estimate; the one-shot benefit and four-week suspension of tactical fouling are separately described for the model to weigh.

The AI remembers recent executed adjustments. With an unchanged score and opponent tactic, small estimated gains cannot reverse a recent tactic or position swap within 20 minutes; a major benefit can override this. An unused tactical-foul setup is not repeatedly toggled on and off for small attribute gains. Structured adjustment history persists in saves.
