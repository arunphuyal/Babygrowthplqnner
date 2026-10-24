using Babybunu.Api.Domain;

namespace Babybunu.Api.Data;

/// <summary>
/// Starter guides seeded on first run. General information only, not medical advice; edit or add more in the admin portal.
/// Style: neutral international English with US spelling, "pee" and "poop", "diaper". Bodies support paragraphs, "## " headings,
/// "- " lists and [links](/guides/slug). Every guide must pass the on-page checklist in StarterGuideQualityTests.
/// </summary>
public static class Guides
{
    private static Article G(string slug, string keyword, string metaTitle, string metaDescription, string title, string summary, string body) => new()
    {
        Slug = slug, FocusKeyword = keyword, MetaTitle = metaTitle, MetaDescription = metaDescription,
        Title = title, Summary = summary, Body = body, IsPublished = true
    };

    public static IEnumerable<Article> All =>
    [
        G("potty-training-readiness", "potty training readiness",
          "Potty Training Readiness: Signs Your Toddler Is Ready",
          "Potty training readiness matters more than age. Learn the physical, behavioral and emotional signs to look for before you start.",
          "Is my toddler ready for potty training?",
          "Readiness matters more than age. Here are the signs to look for before you start.",
"""
Potty training readiness matters more than your child's age. Many children are ready somewhere between 18 and 36 months, but there is no deadline, and starting when your child is truly ready usually means fewer accidents and less stress for everyone.

## Physical signs of readiness
- Stays dry for around two hours or more, or wakes from a nap with a dry diaper
- Has fairly regular, predictable bowel movements
- Can walk to the potty, sit steadily, and pull clothes up and down
- Shows signs of needing to go, such as pausing, squatting or going quiet

## Signs in behavior and communication
- Tells you, or shows you, that they are wet, dirty or about to go
- Shows interest in the toilet or in what other people do there
- Follows simple instructions such as "bring me your shoes"
- Dislikes a wet or dirty diaper and wants it changed

## Emotional signs
Your child wants to do things independently, says "me do it" and takes pride in new skills. If your family is in the middle of a big change such as a new baby, a move or a new daycare, it can be worth waiting a few weeks. Our guide to [potty training and big changes](/guides/potty-training-siblings) explains how to time it.

## What if my child is not ready?
If you start and it turns into a battle, it is fine to pause for a few weeks and try again. Pressure and punishment tend to backfire and can make children resist the potty. Before you begin, spend a few days noting when your child pees and poops while still in diapers. A simple log shows how regular the pattern already is, which makes picking a start day much easier.

## Your next steps
When you notice several signs together, read about [the main potty training methods](/guides/potty-training-methods) and plan [your first week](/guides/potty-training-first-week). Gather the basics from our [potty training equipment checklist](/guides/potty-training-equipment). Wondering what to expect at your child's age? See [potty training at 2](/guides/potty-training-at-2).

If your child learns differently or has developmental or physical needs, read about [adapting the approach](/guides/potty-training-additional-needs). If you have concerns about your child's development or toileting, speak with your doctor or health visitor.
"""),

        G("potty-training-first-week", "potty training first week",
          "Potty Training First Week: A Simple Routine That Works",
          "Plan your potty training first week with a calm routine: preparation, when to offer the potty, handling accidents and using a log.",
          "Your first week: a simple routine that works",
          "Pick a calm week, set up the basics, and use a log to find your child's rhythm.",
"""
A little planning makes the potty training first week much calmer. Choose a week when you are at home more than usual, with few trips or changes planned, and when your child is healthy and in a reasonable mood.

## Before day one
- Get a potty or a toilet seat with a step, and let your child sit on it fully clothed to get used to it (see [equipment](/guides/potty-training-equipment))
- Stock up on easy-to-remove clothes and plenty of spare changes
- Agree on simple words for pee and poop that every caregiver will use
- Tell anyone who helps, such as grandparents or your [daycare](/guides/potty-training-daycare), what you are doing and when

## During the week
- Offer the potty at predictable times: on waking, after meals and drinks, before leaving the house and before bed
- Keep sits short. A few minutes is plenty, and you can read a book or sing a song
- Praise effort as well as success, and stay calm after accidents (more in [handling accidents](/guides/potty-training-accidents))
- Offer plenty of fluids. More drinks mean more chances to practice, and see [drinks and food](/guides/potty-training-drinks-and-food) for how fluids affect timing

## Use your log
Write down every pee, poop, accident and drink. After a few days you can see the usual gap between a drink and a pee, and the times of day your child most often goes. That tells you when to offer the potty, instead of guessing or asking every ten minutes.

## Expect accidents
Accidents are a normal part of learning. Clean up without fuss, change your child, and try again at the next opportunity. Most children need several weeks before daytime accidents become rare.

## When to pause
If your child is upset, refuses the potty for several days, or you both feel stressed, it is fine to go back to diapers for a few weeks and try again. Stepping back is not failing, and many families succeed on the second attempt.
"""),

        G("potty-training-accidents", "potty training accidents",
          "Potty Training Accidents: What Is Normal and What to Do",
          "Potty training accidents are normal. Learn why they happen, how to respond calmly, and what to do when progress stalls.",
          "Accidents and setbacks: what is normal",
          "Why accidents happen, how to respond, and what to do when progress stalls.",
"""
Potty training accidents are part of every child's learning, and they often continue for weeks or even months. They are most common when your child is absorbed in play, tired, excited or ill.

## How to respond to an accident
- Stay calm and matter-of-fact: "Oops, pee goes in the potty. Let's get changed."
- Let your child help clean up if they like, without making it a punishment
- Avoid shaming or scolding, which can make children hide the need to go
- Keep a change of clothes everywhere you go, and a small towel for car seats and strollers

## Why accidents happen
- Your child is busy and ignores the first signal from their body
- The potty is too far away or unfamiliar, for example at a friend's house
- Excitement, tiredness or a growth spurt
- A big change such as a new baby, a move or starting daycare (see [potty training and big changes](/guides/potty-training-siblings))
- Constipation, which can cause leaking around hard stool (read about [toddler constipation](/guides/toddler-constipation-potty-training))

## Look for patterns
Write down when accidents happen. If most come before lunch or right after play, offer the potty earlier and more often at those times. A log also shows whether accidents follow big drinks or happen when your child has gone a long time without trying.

## When progress stalls or goes backward
If a child who was doing well starts having many accidents, you are probably seeing [potty training regression](/guides/potty-training-regression), which is common and usually temporary. Go back to the basics from the [first week](/guides/potty-training-first-week): regular offers, calm reactions and lots of praise for trying.

## When to talk to a doctor
Speak with your doctor if your child seems to be in pain when peeing or pooping, has accidents with a sudden change in thirst or amount of pee, is withholding poop, or if daytime accidents continue well beyond the age you would expect. The doctor can check for causes such as constipation or a urinary infection.
"""),

        G("potty-training-drinks-and-food", "potty training drinks",
          "Potty Training Drinks and Food: How Intake Affects Timing",
          "Potty training drinks and food shape when your child needs to go. Learn how fluids and fiber affect pee, poop and timing.",
          "Drinks, food and poop: what to track and why",
          "How fluids and fiber relate to pee and poop, and how to read your log.",
"""
Potty training drinks and food matter because toileting is closely linked to what goes in. Tracking drinks and meals alongside pees and poops can help you spot patterns and choose better moments to offer the potty.

## Fluids and pee
Most children need to pee some time after a good drink. Over a few days your log can show your child's typical delay, for example about 45 minutes. That is a helpful time to offer the potty. Offering plenty of water through the day also gives your child more chances to practice. Very small sips tell you little, so many parents track only larger drinks.

## Food, fiber and poop
Fiber-rich foods such as fruit, vegetables, oats and whole-grain bread, along with enough water, help keep stools soft and easy to pass. The Bristol stool scale describes stools from type 1 (hard lumps) to type 7 (liquid). Types 3 to 5 are generally considered the easiest to pass. If your child's poop is often hard, read our guide to [toddler constipation](/guides/toddler-constipation-potty-training).

## What to log
- Drinks: roughly how much, or use small, medium and large
- Meals and snacks: add tags like "fiber" or "dairy" if you want to look for patterns later
- Pees and poops: whether they went in the potty, and for poop the stool type

## Timing the potty
After you know the usual gap between a drink and a pee, offer the potty a little before that time. Also try after meals, since eating often triggers a bowel movement. These routines are explained in the [first-week guide](/guides/potty-training-first-week).

## When to ask for advice
Speak with your doctor if your child often has hard or painful stools, goes several days without a poop and seems uncomfortable, has blood in the stool, or has noticeably less pee than usual. Notes from your own log are observations, not medical advice, but they make a useful summary to share at an appointment.
"""),

        G("potty-training-methods", "potty training methods",
          "Potty Training Methods Compared: Which One Suits You?",
          "Compare the main potty training methods: child-led, the 3-day approach and gradual routines, with pros, cons and tips for choosing.",
          "Potty training methods compared",
          "Child-led, the 3-day approach and gradual routines: how they differ and how to choose.",
"""
There is no single best way, and the right potty training methods depend on your child, your schedule and your own patience. Most approaches are variations of three styles.

## Child-led training
You wait for clear readiness signs (see [potty training readiness](/guides/potty-training-readiness)), then follow your child's pace. The potty is available, you offer it at natural moments, and you celebrate successes without pressure.
- Pros: low stress, little resistance
- Cons: can take several weeks or months, and some children need a gentle nudge to get started

## The 3-day or intensive approach
You set aside a few days at home, remove diapers and offer the potty very frequently, watching closely for signals. It is popular because it is focused and has an end date.
- Pros: quick start and clear routine
- Cons: demanding for the parent, and it can go badly if your child is not ready or the week is not calm. Even with this approach, most children keep having accidents for weeks, so expect a gradual finish (see [accidents](/guides/potty-training-accidents))

## Gradual or routine-based training
You build habits step by step: sitting on the potty at set times, such as after meals, then moving to training pants during the day and eventually to underwear.
- Pros: fits busy families and daycare schedules
- Cons: progress can feel slow, and mixed signals are possible if diapers and underwear alternate

## How to choose
- Choose child-led if your child is not yet showing many signs or you want the gentlest path
- Choose intensive if your child is ready, you have several free days and you like structure
- Choose gradual if both parents work or your child spends days at [daycare](/guides/potty-training-daycare)

Whatever you choose, be consistent, keep reactions calm, and use a log to see what is working. Then plan [your first week](/guides/potty-training-first-week). If one approach is not working after a couple of weeks, it is fine to switch.
"""),

        G("potty-training-at-2", "potty training at 2",
          "Potty Training at 2: What to Expect and How to Start",
          "Potty training at 2 can work well if your child shows readiness signs. See what to expect, how long it takes and tips for toddlers.",
          "Potty training at 2: what to expect",
          "What is typical for two-year-olds, how long it can take and how to keep it positive.",
"""
Potty training at 2 is very common, but it is not a race. Some two-year-olds are ready and learn quickly, while others do better a few months later. The best predictor of success is readiness, not the calendar.

## What is typical at age 2
- Many children show early signs between 18 and 30 months
- Girls tend to be ready a little earlier on average, but there is wide variation in both boys and girls
- Daytime dryness often takes several months to become reliable, and night dryness usually comes later (see [night-time potty training](/guides/night-time-potty-training))

## How to tell if your two-year-old is ready
Check the [readiness signs](/guides/potty-training-readiness): staying dry for a couple of hours, telling you when they are wet, interest in the toilet, and following simple instructions. Two-year-olds are also at the age of strong opinions, so if every request is met with "no", a calm approach and some choices (which potty, which underwear) help.

## Making a start
- Introduce the potty casually and let your child sit on it with clothes on
- Offer it after waking, after meals and before bath or bed
- Use short, positive language and simple [rewards](/guides/potty-training-rewards)
- Plan around a quiet week, as described in the [first-week routine](/guides/potty-training-first-week)

## How long does it take?
Some children get the idea in a few days, but most take weeks to a few months to be mostly dry in the daytime. Accidents are normal for a long time afterward, especially when your child is busy or tired.

## If it is not going well
If your child is resisting, crying or hiding, take a break and try again in a few weeks. Pushing a reluctant two-year-old often creates a power struggle. You might also be seeing a temporary setback, which is covered in [potty training regression](/guides/potty-training-regression).

If your child is nearing three and shows no interest at all, or you have any worries about their development, talk with your doctor or health visitor.
"""),

        G("potty-training-regression", "potty training regression",
          "Potty Training Regression: Why It Happens and How to Cope",
          "Potty training regression is common. Learn why a toilet-trained child suddenly has accidents again and how to get back on track calmly.",
          "Potty training regression: what to do",
          "Why a child who was doing well starts having accidents again, and how to get back on track.",
"""
Potty training regression means a child who was doing well suddenly starts having accidents, refusing the potty or holding poop. It is frustrating, but it is very common and usually temporary.

## Common reasons for regression
- A big change at home, such as a new baby, a move, a new daycare or a parent traveling (see [potty training and big changes](/guides/potty-training-siblings))
- Illness, teething, tiredness or a growth spurt
- A scare or a bad experience, such as a painful poop or a loud automatic toilet
- Being too busy to stop playing
- A wish for attention, or for the baby-like care a younger sibling gets
- Constipation, which can make going painful and cause leaks (read about [toddler constipation](/guides/toddler-constipation-potty-training))

## What to do
- Stay calm and avoid punishment, shaming or comparing your child to others
- Go back to basics: offer the potty at regular times and remind your child gently
- Keep routines predictable and give plenty of positive attention for any effort
- Check for pain, illness or constipation and talk to your doctor if you suspect it
- Review your log to see when accidents are happening and what changed that week

## Should I go back to diapers?
Usually it is better not to, because it can feel like a step backward and confuse your child. If you are both very stressed, a short break with pull-ups at certain times, such as outings, is fine. Then restart the routines from the [first-week guide](/guides/potty-training-first-week).

## How long does it last?
Most regressions pass within a few days to a few weeks once the cause settles. Keep your reactions calm, and your child will usually find their way back. Our guide to [accidents and setbacks](/guides/potty-training-accidents) has more ideas.

## When to talk to a doctor
Get advice if your child seems to be in pain, has a fever, pees much more or much less than usual, has blood in pee or poop, or if the regression lasts more than a few weeks without an obvious cause.
"""),

        G("night-time-potty-training", "night time potty training",
          "Night Time Potty Training: When and How Kids Stay Dry",
          "Night time potty training comes after daytime dryness. Learn what is normal, when to start, and how to handle bedwetting calmly.",
          "Night-time dryness: when and how",
          "Why nights come later than days, what is normal, and how to handle bedwetting calmly.",
"""
Night time potty training works differently from daytime training. Staying dry overnight depends on a child's body maturing, not on effort or willpower, so it usually happens later and cannot be rushed.

## What is normal?
- Many children are reliably dry by day around ages 3 to 4, while staying dry at night can take longer
- Bedwetting is common in young children and often runs in families
- Some children stay dry at night soon after they are dry by day, while others take years

## Signs your child may be ready for dry nights
- Several mornings in a row with a dry diaper or pull-up
- Your child wakes up and tells you they are wet or need to go
- They have been dry through the day for a good while (see [potty training readiness](/guides/potty-training-readiness) for daytime signs)

## Tips that help
- Offer the potty or toilet right before bed
- Move most drinks earlier in the day, but do not restrict fluids heavily
- Keep a path to the bathroom lit and easy to walk
- Use waterproof mattress protectors, and keep spare sheets and pajamas ready
- Stay relaxed. Never punish or tease a child for wet nights

## Diapers or pull-ups at night
It is fine to use night-time diapers or pull-ups until your child has a run of dry nights. Switching too early can lead to many wet beds and discouragement. Some families try "lifting" a child to the toilet before they go to bed, but it does not teach the body to wake up, so use it only as a convenience.

## When to see a doctor
Speak with your doctor if your child is still wetting regularly after about age 6 or 7, if dry nights stop after a long dry period, or if bedwetting comes with pain, unusual thirst, snoring or daytime problems. These can be treated, and there is no need to feel embarrassed.

For daytime accidents, see [potty training accidents](/guides/potty-training-accidents).
"""),

        G("toddler-constipation-potty-training", "toddler constipation",
          "Toddler Constipation and Potty Training: Signs and Help",
          "Toddler constipation can make potty training harder. Learn the signs, gentle things that help, and when to talk to your doctor.",
          "Toddler constipation and potty training",
          "Why hard poop makes potty training harder, the signs to watch for, and what can help.",
"""
Toddler constipation is one of the most common reasons potty training stalls. When pooping hurts, children can start to hold it in, which makes the stool harder and the next one more painful.

## Common signs
- Hard, dry, pellet-like or very large stools (Bristol types 1 and 2)
- Pain, crying or straining when pooping
- Going less often than usual or going several days without a poop
- Withholding behavior: standing on tiptoe, clenching, hiding or crossing legs
- Leaking of soft stool or smears in underwear, which can happen around a blockage
- Belly pain or a poor appetite

## Gentle things that often help
- Offer plenty of water through the day, since fluids help stools stay soft
- Include fiber-rich foods such as fruit, vegetables, oats, beans and whole grains (see [drinks and food](/guides/potty-training-drinks-and-food))
- Build a relaxed routine of sitting on the potty for a few minutes after meals, when the body is most likely to go
- Use a footstool so your child's knees are higher than their hips, which makes pooping easier
- Praise any effort, and never punish or rush

## Do not ignore withholding
If your child holds poop because it hurts, pressure to "just go" usually makes it worse. Pause pressure around poop, keep diapers or pull-ups available for it if needed, and talk to your doctor. This is also a common cause of [potty training regression](/guides/potty-training-regression) and accidents.

## What not to do
Do not give laxatives, suppositories or supplements without a doctor's advice. Doses depend on age and weight, and the right treatment depends on the cause.

## When to talk to your doctor
Get advice if constipation lasts more than a couple of weeks, your child has pain, blood in the stool, vomiting, fever, a swollen belly or poor weight gain, or if soft leaks keep happening. A log of dates, stool types and what your child ate and drank helps your doctor see the pattern. This guide is general information, not medical advice.
"""),

        G("potty-training-daycare", "potty training daycare",
          "Potty Training Daycare Tips: Work Together With Caregivers",
          "Potty training daycare tips: how to share routines, what to pack, and how to keep your approach consistent between home and care.",
          "Potty training and daycare",
          "How to share routines with caregivers so your child gets the same approach everywhere.",
"""
Potty training daycare arrangements work best when home and care use the same approach. Children learn faster and feel safer when every adult uses the same words, routines and reactions.

## Talk to the daycare early
- Ask about their potty training policy and whether staff will help
- Explain which method you are using (see [potty training methods](/guides/potty-training-methods)) and when you plan to start
- Agree on simple words for pee and poop, and how staff should react to accidents
- Ask how often children are taken to the toilet during the day, and whether you can request extra offers at set times

## What to pack
- At least three or four complete changes of clothes, plus extra socks and shoes
- Easy clothes your child can pull up and down by themselves, with no overalls or tight belts
- A labeled bag for wet clothes
- Pull-ups for nap time, if your child still needs them

## Share what you learn
A short daily note or message helps caregivers build on what works. Share the times your child usually goes, any accidents, and how they reacted. If you keep a log of pees, poops and accidents, you can use it to spot the time of day that needs extra support. Many apps let you share a summary or give caregivers access directly.

## Handling setbacks
Starting daycare or changing rooms can cause a temporary step backward (see [potty training regression](/guides/potty-training-regression)). Reassure your child, keep the routine the same, and stay in touch with staff. Calm reactions matter most, as covered in the [accidents guide](/guides/potty-training-accidents).

## Other caregivers
The same ideas apply for grandparents, nannies and babysitters. Write down the main points on one page: the words you use, the usual times, rewards, and what to do after an accident.

If you are just getting started, begin on a weekend or a quiet week at home first, as in the [first-week routine](/guides/potty-training-first-week), then bring daycare on board a few days later.
"""),

        G("potty-training-travel", "potty training travel",
          "Potty Training Travel: Tips for Trips, Cars and Outings",
          "Potty training travel does not have to be stressful. Practical tips for car rides, flights, restaurants and visits away from home.",
          "Potty training on the go and traveling",
          "Practical tips for car rides, flights, restaurants and visits away from home.",
"""
Potty training travel can feel daunting, but a little preparation makes outings and trips much easier. The goal is to stay relaxed and keep routines as familiar as possible.

## Before you leave the house
- Offer the potty before you go, even if your child says they do not need to
- Pack a change of clothes for your child, a spare for you, wipes, plastic bags for wet clothes and hand sanitizer
- Bring a portable potty seat or a foldable toilet insert, which many children find more comfortable than a large adult seat
- Plan stops so your child has a chance to try every hour or two, as often as they would at home

## Car trips
Cover the car seat with a waterproof pad and keep a small potty or a travel bag within reach. Stop before long drives and take regular breaks. Many families use pull-ups for long drives and say so calmly, so your child understands it is a special rule for the car, not a step backward.

## Flights and trains
Ask for an aisle seat so you can get to the bathroom quickly, and visit before boarding. Consider pull-ups for the flight itself. Take extra clothes in your carry-on, because luggage can be delayed.

## Restaurants and visits
Find the bathroom as soon as you arrive. Check in with your child after drinks, as described in [drinks and food](/guides/potty-training-drinks-and-food). When staying with relatives, bring your child's own potty and tell the hosts about your routine.

## Public toilets
Loud flushing or automatic sensors can scare small children. Cover the sensor with a sticky note, or let your child flush later after leaving the stall. Teach hand washing every time (see [potty training hygiene](/guides/potty-training-hygiene)).

## If accidents happen
They will, and that is fine. Stay calm, change your child, and carry on. Our guide to [accidents and setbacks](/guides/potty-training-accidents) has more tips. Many families find that a trip is best attempted after the first few weeks of [potty training](/guides/potty-training-first-week) have settled.
"""),

        G("potty-training-equipment", "potty training equipment",
          "Potty Training Equipment: What You Really Need",
          "Potty training equipment checklist: potty chairs, toilet seats, step stools, training pants and extras, and what you can skip.",
          "Potty training equipment checklist",
          "What you actually need, what is nice to have, and what you can skip.",
"""
You do not need much potty training equipment to get started, and most of the extras are optional. Here is a simple checklist to help you decide what is worth buying.

## The essentials
- A potty chair or a toilet seat insert: either works, and some families use both. A stand-alone potty is easy for a small child to use alone and can be kept in the living room at first. A toilet seat insert means no extra equipment to clean or to move to the bathroom later
- A step stool, so your child can climb up and rest their feet on something. Having feet supported makes pooping much easier (see [toddler constipation](/guides/toddler-constipation-potty-training))
- Easy clothing that your child can pull up and down alone
- Spare underwear, pants and socks in several places around the house

## Training pants, pull-ups and underwear
Some families go straight to underwear because it lets children feel wetness and motivates them. Others use cloth training pants or disposable pull-ups at nap time, at night and for outings. There is no right answer. Think about your child's temperament and your tolerance for laundry. Our [methods guide](/guides/potty-training-methods) explains how each approach fits different styles.

## Useful extras
- Waterproof mattress protectors for night time (see [night-time potty training](/guides/night-time-potty-training))
- A portable potty or folding seat for trips
- A small basket of books or toys near the potty
- A simple chart or stickers if you decide to use [rewards](/guides/potty-training-rewards)
- A kid-friendly step stool near the sink for hand washing

## What you can skip
Expensive potties with sounds, lights or games are not necessary and can distract children from the task. Books and videos can be helpful, but they are optional.

## Keep a simple log
A notebook or an app for recording pees, poops, drinks and accidents costs little and often helps more than any gadget, because it shows when your child usually needs to go. Combine it with the routine from the [first-week guide](/guides/potty-training-first-week).
"""),

        G("potty-training-rewards", "potty training rewards",
          "Potty Training Rewards: What Works and What to Avoid",
          "Potty training rewards can motivate toddlers when used well. Learn simple ideas, how to phase them out and mistakes to avoid.",
          "Rewards and praise that work",
          "Simple ideas for sticker charts, praise and small treats, and how to phase them out.",
"""
Potty training rewards can help a child feel proud and motivated, but they work best when they are simple, immediate and used for a short time. Praise is a reward too, and for many children it is the most powerful one.

## Start with praise
Specific praise ("You noticed you needed to go and sat on the potty!") tells your child what to repeat. Keep it warm and calm, not over the top, because loud celebrations can overwhelm some children. Praise effort, such as trying and sitting, and not only success.

## Simple reward ideas
- A sticker on a chart after each successful pee or poop
- A special song, dance or high-five
- A few minutes of a favorite game with you
- A small treat, only if your family is comfortable with it. If you use treats, keep them tiny and avoid sweets as a regular habit

## Tips for using rewards
- Give the reward right away so your child connects it to the action
- Keep it small and consistent
- Reward behavior your child controls, such as sitting and trying, not dry underwear
- Let your child choose the stickers or a chart design
- Do not remove rewards or stickers after accidents, and do not punish (see [potty training accidents](/guides/potty-training-accidents))

## Phasing rewards out
Once your child is using the potty most of the time, move to rewards for longer stretches, such as a sticker for a whole dry morning, and then fade them gradually. Many children lose interest in the chart on their own.

## When rewards do not work
If rewards cause pressure or your child becomes upset about not earning one, simplify or stop. Some children do better with no rewards at all and simply a calm routine, as in the [first-week routine](/guides/potty-training-first-week). If your child is not showing interest, revisit [potty training readiness](/guides/potty-training-readiness) before trying again.

Tracking successes also gives you a clear picture to celebrate: seeing a dry streak grow can be rewarding for parents too.
"""),

        G("potty-training-hygiene", "potty training hygiene",
          "Potty Training Hygiene: Wiping, Hand Washing and Cleanup",
          "Potty training hygiene basics: how to teach wiping, hand washing, cleaning potties and keeping everyone healthy.",
          "Wiping, hand washing and cleanup",
          "Teach wiping, hand washing and cleanup habits that keep everyone healthy.",
"""
Potty training hygiene is part of learning to use the toilet. Teaching wiping, hand washing and cleanup early builds habits that protect your child and the whole family from germs.

## Hand washing every time
Make hand washing a fixed step right after every pee or poop, and before eating. Use soap and running water, rub for about twenty seconds (singing a short song helps), then rinse and dry. A sturdy step stool at the sink lets children do it independently.

## Wiping
- Young children usually need help with wiping for a long time, especially after poop. Do it for them at first, then let them try and finish the job yourself
- Teach girls to wipe from front to back, which helps prevent urinary infections
- Boys usually need to shake or pat dry after peeing
- Use plain toilet paper or fragrance-free wipes, and avoid flushing wipes that are not labeled flushable
- If the skin is red or sore, mention it to your doctor, since it may need a cream or a different routine

## Cleaning the potty
Empty it into the toilet right away, rinse it, and clean it with soap and water or a mild disinfectant. Wash your hands afterward. Keep it in a place that is easy to reach but easy to clean.

## Accidents and laundry
Rinse soiled clothes, put them in a bag or bucket, and wash them on a hot setting when possible. Wash your hands after cleanup, and clean any surfaces that got wet. More tips on staying calm are in our [guide to accidents](/guides/potty-training-accidents).

## When away from home
Carry hand sanitizer for emergencies, but soap and water is better. Public toilets are covered in the [travel and outings guide](/guides/potty-training-travel).

## Looking after skin
Frequent wiping, diapers and wet clothes can irritate skin. Pat dry, change wet clothes quickly, and see a doctor if irritation does not improve.

When your child is ready to try wiping, praise the effort, as described in [potty training rewards](/guides/potty-training-rewards).
"""),

        G("potty-training-siblings", "potty training siblings",
          "Potty Training Siblings and Big Changes: Timing It Right",
          "Potty training siblings and big life changes: how a new baby, a move or a new school affects toilet learning, and when to wait.",
          "Potty training, siblings and big changes",
          "How a new baby, a move or a new routine affects potty training, and how to time it.",
"""
Potty training siblings and big life changes often collide. A new baby, a move or a new daycare can make a child feel unsettled, and toileting is one of the first things to wobble.

## Is it a good time to start?
Think about what else is going on in your child's life. If a major change is coming in the next month, it is usually better to wait until things settle. If your child is already ready (see [potty training readiness](/guides/potty-training-readiness)) and the change is small, it can be fine to continue.

## A new baby in the house
- Try to finish potty training, or wait a while, rather than starting right around the birth
- Expect regression after the baby arrives (see [potty training regression](/guides/potty-training-regression)). Your toddler may want diapers or bottles too, which is normal
- Give your toddler special one-on-one time, and praise their grown-up skills without comparing them with the baby
- Let your child help with small jobs such as choosing diapers for the baby, so they feel included

## Moves, new schools and travel
New surroundings mean a new toilet, new smells and new routines. Show your child where the bathroom is, bring familiar items such as their potty seat, and keep reminders friendly. For trips, see [potty training travel](/guides/potty-training-travel).

## Training more than one child
If you have two children close in age, train them one at a time when possible, and avoid comparing them. An older sibling can be a good role model, but some younger children copy their sibling too early or feel pressured, so let your younger child lead.

## Keeping your own energy up
Potty training takes patience. Share the work with partners and other caregivers, and use a [simple routine](/guides/potty-training-first-week) so everyone does the same thing. If you feel overwhelmed, it is fine to pause. A calm start a few weeks later is better than a stressful start now.

If your child's behavior changes sharply for more than a few weeks, talk to your doctor or child health nurse.
"""),

        G("potty-training-additional-needs", "potty training additional needs",
          "Potty Training Additional Needs: Adapting the Approach",
          "Potty training additional needs: how to adapt readiness, routines and support for children with developmental or sensory differences.",
          "Potty training a child with additional needs",
          "How to adapt readiness, routines and support when a child learns differently.",
"""
Potty training additional needs looks different for every child. Children with developmental delays, autism, sensory differences, physical disabilities or other needs can absolutely learn to use the toilet, but the timing and the approach often need adapting.

## Readiness may look different
Many of the usual signs, such as telling you they are wet, may appear later or in different ways. Look for other cues, such as pausing, changes in behavior, or regular patterns in when your child goes. A log of pees and poops can show routines even when your child cannot tell you (read about [potty training readiness](/guides/potty-training-readiness) for the general signs).

## Ideas that often help
- Use clear, consistent routines and the same words each time, and the same place and the same steps
- Add visual supports, such as picture sequences showing the steps of using the toilet
- Break the process into small steps and practice one at a time
- Think about sensory comfort: seat height, cold surfaces, noisy flushes, textures of clothing or wipes
- Use rewards that your child truly values, and keep them short (see [potty training rewards](/guides/potty-training-rewards))
- Allow extra time, and avoid comparing your child with others

## Physical support
Children with physical disabilities may need adapted seats, supports for sitting, step stools or help with clothing. A physical therapist or occupational therapist can recommend equipment and positions (see the [equipment guide](/guides/potty-training-equipment)).

## Work with professionals
Ask your doctor, early intervention team, school or therapists for advice. They may suggest approaches built for your child, and can check for medical causes such as constipation, which is common and can make progress harder (see [toddler constipation](/guides/toddler-constipation-potty-training)).

## Be patient and celebrate progress
Progress may be slower and may come in small steps, such as sitting for a few seconds. Celebrate every step, and share the plan with everyone who cares for your child, including [daycare or school](/guides/potty-training-daycare), so the approach is consistent.

Every child learns at their own pace, and with patience and the right support most children get there.
"""),
    ];
}
