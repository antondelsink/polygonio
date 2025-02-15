﻿namespace PolygonIo.WebSocket.Contracts
{
    // as per https://polygon.io/glossary/us/stocks/conditions-indicators
    public enum QuoteCondition
    {
        Regular = 0,
        RegularTwoSidedOpen = 1,
        RegularOneSidedOpen = 2,
        SlowAsk = 3,
        SlowBid = 4,
        SlowBidAsk = 5,
        SlowDueLRPBid = 6,
        SlowDueLRPAsk = 7,
        SlowDueNYSELRP = 8,
        SlowDueSetSlowListBidAsk = 9,
        ManualAskAutomaticBid = 10,
        ManualBidAutomaticAsk = 11,
        ManualBidAndAsk = 12,
        Opening = 13,
        Closing = 14,
        Closed = 15,
        Resume = 16,
        FastTrading = 17,
        TradingRangeIndication = 18,
        MarketMakerQuotesClosed = 19,
        NonFirm = 20,
        NewsDissemination = 21,
        OrderInflux = 22,
        OrderImbalance = 23,
        DueToRelatedSecurityNewsDissemination = 24,
        DueToRelatedSecurityNewsPending = 25,
        AdditionalInformation = 26,
        NewsPending = 27,
        AdditionalInformationDueToRelatedSecurity = 28,
        DueToRelatedSecurity = 29,
        InViewOfCommon = 30,
        EquipmentChangeover = 31,
        NoOpenNoResume = 32,
        SubPennyTrading = 33,
        AutomatedBidNoOfferNoBid = 34,
        LuldPriceBand = 35,
        MarketWideCircuitBreakerLevel1 = 36,
        MarketWideCircuitBreakerLevel2 = 37,
        MarketWideCircuitBreakerLevel3 = 38,
        RepublishedLuldPriceBand = 39,
        OnDemandAuction = 40,
        CashOnlySettlement = 41,
        NextDaySettlement = 42,
        LULDTradingPause = 43,
        SlowDueLRPBidAsk = 71
    }
}
