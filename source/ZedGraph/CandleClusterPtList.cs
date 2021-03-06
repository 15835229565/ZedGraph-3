﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ZedGraph
{
  /// <summary>
  /// A collection class containing a list of <see cref="CandleClusterPt"/> objects
  /// that define the set of points to be displayed on the curve.
  /// </summary>
  [Serializable]
  public class CandleClusterPtList : List<CandleClusterPt>, IPointListEdit, IOrdinalPointList
  {
    private readonly List<Tuple<double, int>> _dateIndex;
    private int _offset;

    #region Properties

    /// <summary>
    /// Indexer to access the specified <see cref="StockPt"/> object by
    /// its ordinal position in the list.
    /// </summary>
    /// <param name="index">The ordinal position (zero-based) of the
    /// <see cref="StockPt"/> object to be accessed.</param>
    /// <value>A <see cref="StockPt"/> object reference.</value>
    public new IPointPair this[int index]
    {
      get { return base[index]; }
      set
      {
        var v = new CandleClusterPt(value);
        if (_dateIndex != null)
        {
          if (Math.Abs(base[index].Date - v.Date) > 1e-9)
            throw new ArgumentException
              ($"Cannot change date on item#{index}: {new XDate(base[index].Date)} to {new XDate(v.Date)}");
        }
        base[index] = v;
      }
    }

    /// <summary>
    /// Indexer for getting the index that corresponds to given date
    /// if the list contains ordinal dates.
    /// </summary>
    int IOrdinalPointList.Ordinal(double date)
    {
      var idx = indexOf(date);
      if (idx < 0 || idx == _dateIndex.Count) return -1;
      return _dateIndex[idx].Item2 - _offset;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor for the collection class
    /// </summary>
    public CandleClusterPtList() : this(false) {}

    /// <summary>
    /// Constructor for the collection class that can enable ordinal index
    /// </summary>
    public CandleClusterPtList(bool ordinal)
    {
      _offset = 0;
      if (ordinal)
        _dateIndex = new List<Tuple<double, int>>();
    }

    /// <summary>
    /// The Copy Constructor
    /// </summary>
    /// <param name="rhs">The StockPointList from which to copy</param>
    public CandleClusterPtList(CandleClusterPtList rhs)
      : this(rhs._dateIndex != null)
    {
      _offset = 0;

      foreach (var pp in rhs.Cast<ICandleClusteredVolume>().Select(p => new CandleClusterPt(p)))
      {
        Add(pp);

        if (rhs._dateIndex == null) continue;

        _dateIndex.Add(new Tuple<double, int>(pp.Date, Count - 1));
      }
    }

    /// <summary>
    /// Implement the <see cref="ICloneable" /> interface in a typesafe manner by just
    /// calling the typed version of <see cref="Clone" />
    /// </summary>
    /// <returns>A deep copy of this object</returns>
    object ICloneable.Clone()
    {
      return Clone();
    }

    /// <summary>
    /// Typesafe, deep-copy clone method.
    /// </summary>
    /// <returns>A new, independent copy of this class</returns>
    public CandleClusterPtList Clone()
    {
      return new CandleClusterPtList(this);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Add a <see cref="StockPt"/> object to the collection at the end of the list.
    /// </summary>
    /// <param name="point">The <see cref="StockPt"/> object to
    /// be added</param>
    public new void Add(CandleClusterPt point)
    {
      base.Add(new CandleClusterPt(point));
      if (_dateIndex == null) return;

      var date = point.Date;
      if (_dateIndex.Count == 0 || date > _dateIndex[_dateIndex.Count - 1].Item1)
        _dateIndex.Add(new Tuple<double, int>(date, _offset + Count - 1));
      else
        throw new ArgumentException($"Dates in time-series must be chronologically increasing ({date})");
    }

    /// <summary>
    /// Add a <see cref="PointPair"/> object to the collection at the end of the list.
    /// </summary>
    /// <param name="point">The <see cref="PointPair"/> object to be added</param>
    public void Add(IPointPair point)
    {
      //      throw new ArgumentException( "Error: Only the StockPt type can be added to StockPointList" +
      //        ".  An ordinary PointPair is not allowed" );
      base.Add(new CandleClusterPt(point));
    }

    /// <summary>
    /// Add a <see cref="StockPt"/> object to the collection at the end of the list using
    /// the specified values.  The unspecified values (low, open, close) are all set to
    /// <see cref="PointPairBase.Missing" />.
    /// </summary>
    /// <param name="date">An <see cref="XDate" /> value</param>
    /// <param name="high">The high value for the day</param>
    /// <returns>The zero-based ordinal index where the point was added in the list.</returns>
    public void Add(double date, double high)
    {
      Add(new CandleClusterPt(date, PointPair.Missing, (float)high, PointPair.Missing, PointPair.Missing, 0, 0));
    }

    /// <summary>
    /// Add a single point to the <see cref="PointPairList"/> from values of type double.
    /// </summary>
    /// <param name="date">An <see cref="XDate" /> value</param>
    /// <param name="open">The opening value for the day</param>
    /// <param name="high">The high value for the day</param>
    /// <param name="low">The low value for the day</param>
    /// <param name="close">The closing value for the day</param>
    /// <param name="volBuy">The trading buy volume for the day</param>
    /// <param name="volSell">The trading sell volume for the day</param>
    /// <returns>The zero-based ordinal index where the point was added in the list.</returns>
    public void Add(double date, float open, float high, float low, float close, int volBuy, int volSell)
    {
      Add(new CandleClusterPt(date, open, high, low, close, volBuy, volSell));
    }

    /// <summary>
    /// Access the <see cref="StockPt" /> at the specified ordinal index.
    /// </summary>
    /// <remarks>
    /// To be compatible with the <see cref="IPointList" /> interface, the
    /// <see cref="StockPointList" /> must implement an index that returns a
    /// <see cref="PointPair" /> rather than a <see cref="StockPt" />.  This method
    /// will return the actual <see cref="StockPt" /> at the specified position.
    /// </remarks>
    /// <param name="index">The ordinal position (zero-based) in the list</param>
    /// <returns>The specified <see cref="StockPt" />.
    /// </returns>
    public CandleClusterPt GetAt(int index)
    {
      return base[index];
    }

    public new void RemoveAt(int index)
    {
      if (_dateIndex != null)
      {
        if (index > 0 || index < Count - 1)
          throw new ArgumentException
            ($"Cannot remove intermediate data points (index={index}, offset={_offset}, count={Count})");

        var point = this[index];
        var date = point.X;

        var idx = indexOf(date);
        if (idx > -1 && idx < _dateIndex.Count)
          _dateIndex.RemoveAt(idx);

        if (index == 0)
          _offset++;
      }

      base.RemoveAt(index);
    }

    public new void Clear()
    {
      base.Clear();
      _dateIndex?.Clear();
    }

    #endregion

    /// <summary>
    /// Returns an index of the first element which does not compare less than date value.
    /// </summary>
    public CandleClusterPt IndexOf(double date)
    {
      var idx = indexOf(date);
      if (idx < 0 || idx == _dateIndex.Count) return null;
      var i = _dateIndex[idx].Item2 - _offset;
      return base[i];
    }

    private int indexOf(double date)
    {
      if (_dateIndex == null) return -1;
      if (_dateIndex.Count == 0) return 0;

      //var comp = Comparer<T>.Default;
      int lo = 0, hi = Count - 1;
      while (lo < hi)
      {
        var m = lo + (hi - lo) / 2;
        if (DoubleComparer.LT(_dateIndex[m].Item1, date))
          lo = m + 1;
        else
          hi = m - 1;
      }
      return DoubleComparer.LT(_dateIndex[lo].Item1, date) ? lo + 1 : lo;
    }

    public new IEnumerator<IPointPair> GetEnumerator()
    {
      return base.GetEnumerator();
    }
  }
}
