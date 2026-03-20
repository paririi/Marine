using System;
using System.Collections.Generic;

[Serializable]
public class QuizQuestion
{
    public string difficulty;
    public string question;
    public string[] options;
    public int correctAnswerIndex;
    public string correctExplanation;
    public string wrongExplanation;
}

[Serializable]
public class QuizQuestionList
{
    public List<QuizQuestion> questions;
}