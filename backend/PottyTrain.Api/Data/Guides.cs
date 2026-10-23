using PottyTrain.Api.Domain;

namespace PottyTrain.Api.Data;

/// <summary>Starter guides seeded on first run. General information only, not medical advice; edit or add more in the admin portal.</summary>
public static class Guides
{
    private static Article G(string slug, string title, string summary, string body) =>
        new() { Slug = slug, Title = title, Summary = summary, Body = body, IsPublished = true };

    public static IEnumerable<Article> All =>
    [
        G("signs-of-readiness", "Is my toddler ready for potty training?",
          "Readiness matters more than age. Here are the signs to look for before you start.",
"""
Most children are ready somewhere between 18 and 36 months, but there is no deadline. Starting when your child is ready usually means fewer accidents and less stress for everyone.

## Signs of physical readiness
- Stays dry for around two hours or more, or wakes from a nap dry
- Has fairly regular, predictable bowel movements
- Can walk to the potty, pull clothes up and down, and sit steadily

## Signs of readiness in behaviour
- Tells you, or shows you, that they are wet, dirty or about to go
- Shows interest in the toilet or in what other people do there
- Follows simple instructions and wants to do things independently
- Dislikes a wet or dirty nappy

## What if my child is not ready?
If you start and it turns into a battle, it is fine to pause for a few weeks and try again. Pressure and punishment tend to backfire. Logging a few days in the app before you begin can show you how regular your child's pattern already is, which makes picking a start day much easier.

If you have concerns about your child's development or toileting, speak with your paediatrician or health visitor.
"""),

        G("first-week-routine", "Your first week: a simple routine that works",
          "Pick a calm week, set up the basics, and use your log to find your child's rhythm.",
"""
A little preparation goes a long way. Choose a week when you are at home more than usual, with few trips or changes planned.

## Before day one
- Get a potty or toilet seat with a step, and let your child sit on it fully clothed to get used to it
- Stock up on easy-to-remove clothes and spare changes of clothes
- Decide on simple words for wee and poo that every caregiver will use

## During the week
- Offer the potty at predictable times: on waking, after meals and drinks, and before leaving the house or going to bed
- Keep sits short, a few minutes is plenty, and make them relaxed
- Praise effort as well as success. Staying calm after accidents matters most
- Offer plenty of fluids. More drinks mean more chances to practise

## Use your log
Record every wee, poo, accident and drink. After a few days your insights will show the usual gap between drinks and wees, and the times of day your child most often goes. That tells you when to offer the potty, rather than guessing.

## Expect accidents
Accidents are a normal part of learning. Clean up without fuss and try again at the next opportunity.
"""),

        G("accidents-and-setbacks", "Accidents and setbacks: what is normal",
          "Why accidents happen, how to respond, and what to do when progress stalls.",
"""
Accidents are part of every child's potty training, and they often continue for weeks or months, especially when your child is absorbed in play, tired or excited.

## Responding to an accident
- Stay calm and matter-of-fact: "Oops, wees go in the potty. Let's get changed."
- Let your child help clean up if they like, without making it a punishment
- Avoid shaming or scolding, which can make children hide the need to go

## Common reasons for setbacks
- A big change: a new sibling, a house move, starting nursery or school
- Illness, tiredness or a growth spurt
- Holding on to poo because it is uncomfortable or frightening to go
- Being too busy to stop playing

## When progress stalls
Take a step back, offer the potty more regularly, and look at your log for patterns, such as accidents clustering at one time of day. If your child seems to be in pain, is withholding stools, passes very hard stools, or you notice anything that worries you, talk to your paediatrician. The app highlights patterns but cannot diagnose anything.

Most children get there with patience, and setbacks do pass.
"""),

        G("drinks-food-and-poo", "Drinks, food and poo: what to track and why",
          "How fluids and fibre relate to wees and stools, and how to read your log.",
"""
Toileting is closely linked to what goes in. Tracking drinks and meals alongside wees and poos can help you spot patterns.

## Fluids and wees
Most children need to wee some time after a good drink. Over a few days your log can show your child's typical delay, for example about 45 minutes. That is a helpful time to offer the potty.

## Food, fibre and stools
Fibre-rich foods such as fruit, vegetables, oats and wholegrain bread, along with enough water, help keep stools soft and easy to pass. The Bristol stool scale describes stools from type 1 (hard lumps) to type 7 (liquid). Types 3 to 5 are generally considered the easiest to pass.

## What to log
- Drinks: roughly how much, or use small, medium and large
- Meals and snacks: add tags like "fibre" or "dairy" if you want to look for patterns
- Wees and poos: whether in the potty, and for poos the stool type

## When to ask for advice
Speak with your paediatrician if your child often has hard or painful stools, goes several days without a poo and seems uncomfortable, has blood in their stool, or has noticeably fewer wees than usual. the app's notes are observations from your own log, not medical advice.
"""),
    ];
}
