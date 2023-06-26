﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows;
using Syncfusion.WinForms.ListView;
using Syncfusion.WinForms.ListView.Enums;

namespace actions_with_costs
{
    public partial class Form1 : Form
    {
        // View sections
        private FluentActionView fluentActionView;
        private ActionModelView actionModelView;

        // Stored state
        public List<string> allFluents;
        public List<string> positiveNegativeFluents;
        public List<string> allActions;
        public List<Statement> allStatements;

        public Form1()
        {
            InitializeComponent();
            allFluents = new List<string>();
            allActions = new List<string>();
            allStatements = new List<Statement>();
            positiveNegativeFluents = new List<string>();

            // Initializing part of the view responsible for actions and fluents
            fluentActionView = new FluentActionView(
                ref addFluentTextBox,
                ref addActionTextBox,
                ref addFluentButton,
                ref addActionButton,
                ref deleteFluentButton,
                ref deleteActionButton,
                ref removeAllFluents,
                ref removeAllActions,
                ref allFluentsCheckBox,
                ref allActionsCheckBox);

            // Initializing part of the view responsible for creating action model
            actionModelView = new ActionModelView(
                ref statementsPanel,
                ref statementsComboBox,
                ref positiveNegativeFluents,
                ref allStatementsCheckBox,
                ref inconsistentDomainLabel,
                ref deleteStatementButton,
                ref addStatementButton,
                ref executeProgramComboBox,
                ref initialStateProgramComboBox,
                ref executeProgramButton);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            addFluentButton.Enabled = false;
            addActionButton.Enabled = false;
            //addStatementButton.Enabled = false;
            deleteFluentButton.Enabled = false;
            deleteActionButton.Enabled = false;
            deleteStatementButton.Enabled = false;

            List<Item> items = new List<Item>();
            items.Add(new Item() { Text = "Initially statement", Value = "initially" });
            items.Add(new Item() { Text = "Value statement", Value = "value" });
            items.Add(new Item() { Text = "Effect statement", Value = "effect" });

            statementsComboBox.DataSource = items;
            statementsComboBox.DisplayMember = "Text";
            statementsComboBox.ValueMember = "Value";

            actionModelView.initiallyStatementObject.createStatementObject(statementsPanel);
        }

        // ----------------------------- FORM METHODS OF FLUENT/ACTION SECTION ---------------------------------------------
        private void addFluentTextBox_TextChanged(object sender, EventArgs e) => 
            fluentActionView.updateAddButtonState(ModelElementType.FLUENT);
        private void addActionTextBox_TextChanged(object sender, EventArgs e) => 
            fluentActionView.updateAddButtonState(ModelElementType.ACTION);

        private void addFluentTextBox_KeyPress(object sender, KeyPressEventArgs e) => 
            fluentActionView.addModelItemAfterEnter(ref e, ModelElementType.FLUENT, allFluents, buildPositiveNegativeFluents);
        private void addActionTextBox_KeyPress(object sender, KeyPressEventArgs e) => 
            fluentActionView.addModelItemAfterEnter(ref e, ModelElementType.ACTION, allActions, updateCausesDropdown);

        private void addFluentButton_Click(object sender, EventArgs e) =>
            fluentActionView.addFluent(buildPositiveNegativeFluents, allFluents);
        private void addActionButton_Click(object sender, EventArgs e) =>
            fluentActionView.addAction(updateCausesDropdown, allActions);

        private void deleteFluentButton_Click(object sender, EventArgs e) =>
            fluentActionView.deleteModelElement(ModelElementType.FLUENT, ref allFluents, buildPositiveNegativeFluents);
        private void deleteActionButton_Click(object sender, EventArgs e) =>
            fluentActionView.deleteModelElement(ModelElementType.ACTION, ref allActions, updateCausesDropdown);

        private void removeAllFluents_Click(object sender, EventArgs e) =>
            fluentActionView.deleteAllModelElementsOfType(ModelElementType.FLUENT, allFluents, buildPositiveNegativeFluents);
        private void removeAllActions_Click(object sender, EventArgs e) =>
            fluentActionView.deleteAllModelElementsOfType(ModelElementType.ACTION, allActions, updateCausesDropdown);

        private void allFluentsCheckBox_ItemChecked(object sender, ItemCheckEventArgs e) => 
            fluentActionView.updateRemoveButtonState(ModelElementType.FLUENT, e);
        private void allActionsCheckBox_ItemChecked(object sender, ItemCheckEventArgs e) => 
            fluentActionView.updateRemoveButtonState(ModelElementType.ACTION, e);
        private void executeProgramButton_Click(object sender, EventArgs e)
        {
            List<CausesStatement> causesStatements = allStatements
              .FindAll(statement => statement.Type == StatementType.CAUSES)
              .Cast<CausesStatement>()
              .ToList();

            int currentCost = 0;
            List<Literal> initialState = new List<Literal>();
            List<Literal> currentState = new List<Literal>();
            string[] fluents = initialStateProgramComboBox.Text.Split(',');
            foreach(string f in fluents)
            {
                initialState.Add(new Literal(f.Replace("~", ""), f.Contains("~")));
                currentState.Add(new Literal(f.Replace("~", ""), f.Contains("~")));
            }

            List<string> actionsList = new List<string>();
            string[] actions = executeProgramComboBox.Text.Split(',');
            foreach (string a in actions)
            {
                actionsList.Add(a);
            }

            foreach(string action in actionsList)
            {
                List<Literal> allPostconditions = new List<Literal>();
                List<CausesStatement> matchedCausesStatements = causesStatements
                    .FindAll(statement => statement.Action == action)
                    .ToList();

                foreach (CausesStatement statement in matchedCausesStatements)
                {
                    if(statement.Precondition.Count == 0)
                    {
                        allPostconditions.Add(statement.Postcondition);
                        currentCost += statement.Cost;
                    }
                    else
                    {
                        bool ifPreconditionHolds = statement.Precondition.All(l => l.ExistsInCollection(currentState));
                        if (ifPreconditionHolds)
                        {
                            allPostconditions.Add(statement.Postcondition);
                            currentCost += statement.Cost;
                        }
                    }
                }
                foreach(Literal literal in allPostconditions)
                {
                    currentState
                       .Where(l => l.Fluent == literal.Fluent)
                       .Select(l => l.IfHolds = literal.IfHolds)
                       .ToList();
                }
            }
            string state = String.Empty;
            foreach (Literal l in currentState)
            {
                state += ",";
                state += l.ToString();
            }
            state = state.Remove(0, 1);
            FinalStateLabel.Text = "FINAL STATE: " + state;
            FinalCostLabel.Text = "FINAL COST: " + currentCost.ToString();

        }

        // -------------------------------------------------------------------------------------------------------------------


        // ----------------------------- FORM METHODS OF ACTION MODEL --------------------------------------------------------

        private void statementsComboBox_SelectionChangeCommitted(object sender, EventArgs e) =>
            actionModelView.createStatementObject();

        private void addStatementButton_Click(object sender, EventArgs e)
        {
            actionModelView.addStatement(ref allStatements, allFluents);

            List<InitiallyStatement> initiallyStatements = allStatements
            .FindAll(statement => statement.Type == StatementType.INITIALLY)
            .Cast<InitiallyStatement>()
            .ToList();

            List<string> allInitialStatesStringified = new List<string>();
            List<State> allInitialStates = actionModelView.getInitialStates(initiallyStatements, allFluents);
            foreach(State s in allInitialStates)
            {
                string state = String.Empty;
                foreach(Literal l in s.Literals)
                {
                    state += ",";
                    state += l.ToString();
                }
                state = state.Remove(0, 1);
                allInitialStatesStringified.Add(state);
            }
            initialStateProgramComboBox.DataSource = allInitialStatesStringified;
            initialStateProgramComboBox.SelectedItems.Clear();
        }

        private void allStatementsCheckBox_ItemCheck(object sender, ItemCheckEventArgs e) =>
            actionModelView.updateRemoveButtonState(e);

        private void deleteStatementButton_Click(object sender, EventArgs e) => 
            actionModelView.deleteModelElement(ref allStatements);

        // -------------------------------------------------------------------------------------------------------------------

        // ----------------------------- FORM METHODS OF VISUALIZATION --------------------------------------------------------

        private void visualizationButton_Click(object sender, EventArgs e)
        {
            StateDiagram stateDiagramForm = new StateDiagram(allFluents, allActions, allStatements);
            stateDiagramForm.Show();
        }

        // -------------------------------------------------------------------------------------------------------------------

        // ----------------------------- COMMON HELPER METHODS ---------------------------------------------------------------
        private void updateCausesDropdown()
        {
            actionModelView.effectStatementObject.causesAction.Items.Clear();
            actionModelView.effectStatementObject.causesAction.Items.AddRange(allActions.ToArray());
            actionModelView.afterStatementObject.afterActions.DataSource = allActions.ToList();

            actionModelView.programExecuteComboBox.DataSource = allActions.ToList();
            actionModelView.programExecuteComboBox.SelectedItems.Clear();
        }

        private void buildPositiveNegativeFluents()
        {
            positiveNegativeFluents = new List<string>();
            foreach (string item in allFluents)
            {
                positiveNegativeFluents.Add(item);
                string negated = "~" + item;
                positiveNegativeFluents.Add(negated);
            }
            actionModelView.initiallyStatementObject.initiallyComboBox.Items.Clear();
            actionModelView.initiallyStatementObject.initiallyComboBox.Items.AddRange(positiveNegativeFluents.ToArray());

            actionModelView.afterStatementObject.afterPostCondition.Items.Clear();
            actionModelView.afterStatementObject.afterPostCondition.Items.AddRange(positiveNegativeFluents.ToArray());

            actionModelView.effectStatementObject.causesPostcondition.Items.Clear();
            actionModelView.effectStatementObject.causesPostcondition.Items.AddRange(positiveNegativeFluents.ToArray());

            actionModelView.effectStatementObject.causesPrecondition.DataSource = positiveNegativeFluents.ToList();
            actionModelView.effectStatementObject.causesPrecondition.SelectedItems.Clear();
        }
    }

    public class Item
    {
        public Item() { }
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
