import unittest

from app.models import ActionTaken, Intent, PolicyRoute, Sentiment, Urgency
from app.models import IntakeAssessment
from app.policy import SupportPolicyRules


class SupportPolicyRulesTests(unittest.TestCase):
    def setUp(self) -> None:
        self.rules = SupportPolicyRules("handbook")

    def test_parse_request_extracts_headers_and_signals(self) -> None:
        request = self.rules.parse_request(
            "From: ada@example.com\nSubject: API access\n\nDoes Basic include API access?"
        )

        self.assertEqual(request.sender, "ada@example.com")
        self.assertEqual(request.subject, "API access")
        self.assertIn("api-question", request.detected_signals)
        self.assertEqual(request.normalized_text, "Does Basic include API access?")

    def test_api_basic_routes_to_direct_reply(self) -> None:
        request = self.rules.parse_request("Does the Basic plan include API access?")
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(
                Intent.Question,
                Sentiment.Neutral,
                Urgency.Low,
                missing_information=["account id"],
            ),
        )

        self.assertEqual(decision.route, PolicyRoute.Reply)
        self.assertEqual(decision.action_taken, ActionTaken.ReplySent)
        self.assertTrue(any("Premium-only" in policy for policy in decision.applied_policies))

    def test_missing_duplicate_charge_details_routes_to_clarification(self) -> None:
        request = self.rules.parse_request("I think I was charged twice but I can't tell what happened.")
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(Intent.Refund, Sentiment.Confused, Urgency.Medium),
        )

        self.assertEqual(decision.route, PolicyRoute.Clarification)
        self.assertEqual(decision.action_taken, ActionTaken.ClarificationRequested)
        self.assertIn("Wait for the customer", decision.recommended_next_action or "")

    def test_first_month_refund_routes_to_refund_ticket(self) -> None:
        request = self.rules.parse_request(
            "I signed up for Premium a few days ago, barely used it, paid $29, and want a refund."
        )
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(Intent.Refund, Sentiment.Frustrated, Urgency.Medium),
        )

        self.assertEqual(decision.route, PolicyRoute.RefundOrCancellation)
        self.assertEqual(decision.action_taken, ActionTaken.RefundTicketCreated)
        self.assertTrue(any("5-7 business days" in policy for policy in decision.applied_policies))

    def test_explicit_cancellation_routes_to_cancellation_ticket(self) -> None:
        request = self.rules.parse_request("Please cancel my account today.")
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(Intent.Cancellation, Sentiment.Neutral, Urgency.Low),
        )

        self.assertEqual(decision.route, PolicyRoute.RefundOrCancellation)
        self.assertEqual(decision.action_taken, ActionTaken.CancellationTicketCreated)

    def test_manager_request_routes_to_escalation(self) -> None:
        request = self.rules.parse_request("I want a manager to review this chargeback issue.")
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(Intent.Complaint, Sentiment.Angry, Urgency.High),
        )

        self.assertEqual(decision.route, PolicyRoute.Escalation)
        self.assertEqual(decision.action_taken, ActionTaken.EscalatedToHuman)

    def test_cross_account_disclosure_routes_to_direct_refusal(self) -> None:
        request = self.rules.parse_request("Can you tell me what plan my colleague is on?")
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(Intent.Question, Sentiment.Neutral, Urgency.Low),
        )

        self.assertEqual(decision.route, PolicyRoute.Reply)
        self.assertEqual(decision.action_taken, ActionTaken.ReplySent)
        self.assertTrue(any("Do not share customer information" in policy for policy in decision.applied_policies))

    def test_general_venting_does_not_create_billing_clarification(self) -> None:
        request = self.rules.parse_request(
            "honestly just really disappointed. the product used to be great and lately nothing works like it used to."
        )
        decision = SupportPolicyRules.evaluate_policy(
            request,
            _intake(
                Intent.Complaint,
                Sentiment.Frustrated,
                Urgency.Medium,
                missing_information=["order or transaction details"],
            ),
        )

        self.assertEqual(decision.route, PolicyRoute.Reply)
        self.assertEqual(decision.action_taken, ActionTaken.ReplySent)
        self.assertTrue(any("do not ask for billing" in policy for policy in decision.applied_policies))


def _intake(
    intent: Intent,
    sentiment: Sentiment,
    urgency: Urgency,
    missing_information: list[str] | None = None,
) -> IntakeAssessment:
    return IntakeAssessment(
        primary_intent=intent,
        sentiment=sentiment,
        urgency=urgency,
        missing_information=missing_information or [],
        escalation_signals=[],
        summary="Test summary.",
        confidence_notes="Test confidence.",
    )


if __name__ == "__main__":
    unittest.main()
